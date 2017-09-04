﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Npgsql;
using SaltVault.Core.People;

namespace SaltVault.Core.Bills
{
    public interface IBillRepository
    {
        List<Bill> GetAllBasicBillDetails();
        Bill GetBasicBillDetails(int billId);
        int AddBill(AddBillRequest bill);
        bool UpdateBill(UpdateBillRequestV2 billRequest);
        bool DeleteBill(int billId);
        BillPayment GetPayment(int paymentId);
        void AddPayment(AddPaymentRequestV2 paymentRequest);
        bool UpdatePayment(UpdatePaymentRequestV2 paymentRequest);
        bool DeletePayment(int paymentRequestPaymentId);
        List<Person> GetAllPeople();
        List<Person> GetPeople(List<int> peopleIds);
    }

    public class BillRepository : IBillRepository
    {
        private readonly NpgsqlConnection _connection;

        public BillRepository()
        {
            var connectionString = File.ReadAllText("./Data/Config/LIVEConnectionString.config");
            _connection = new NpgsqlConnection(connectionString);
        }

        public List<Bill> GetAllBasicBillDetails()
        {
            _connection.Open();

            var bills = new List<Bill>();
            try
            {
                var command = new NpgsqlCommand("SELECT Bill.\"Id\", Bill.\"Name\", Bill.\"Amount\", Bill.\"Due\", Person.\"Id\", Person.\"Image\", Payment.\"Id\", Payment.\"Amount\", Payment.\"Created\" " +
                                                "FROM public.\"Bill\" AS Bill " +
                                                "LEFT OUTER JOIN \"PeopleForBill\" AS PeopleForBill ON PeopleForBill.\"BillId\" = Bill.\"Id\" " +
                                                "LEFT OUTER JOIN \"Person\" AS Person ON Person.\"Id\" = PeopleForBill.\"PersonId\" " +
                                                "LEFT OUTER JOIN \"Payment\" AS Payment ON Payment.\"BillId\" = Bill.\"Id\" AND Payment.\"PersonId\" = Person.\"Id\" " +
                                                "ORDER BY Bill.\"Due\" DESC", _connection);
                var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    var billId = Convert.ToInt32(reader[0]);
                    Bill billOverview;

                    if (bills.Any(x => x.Id == billId))
                        billOverview = bills.First(x => x.Id == billId);
                    else
                    {
                        billOverview = new Bill
                        {
                            Id = billId,
                            Name = (string)reader[1],
                            TotalAmount = Convert.ToDecimal(reader[2]),
                            FullDateDue = (DateTime)reader[3]
                        };
                    }

                    var personId = Convert.ToInt32(reader[4]);
                    var paymentAmount = (reader[7] == DBNull.Value) ? 0 : Convert.ToDecimal(reader[7]);
                    if (billOverview.People.Any(x => x.Id == personId) == false)
                    {
                        billOverview.People.Add(new BillPersonDetails
                        {
                            Id = personId,
                            Paid = paymentAmount != 0,
                            ImageLink = (string)reader[5]
                        });
                    }
                    else
                        billOverview.People.First(x => x.Id == personId).Paid = true;

                    if (reader[6] != DBNull.Value)
                    {
                        billOverview.Payments.Add(new BillPayment
                        {
                            Id = Convert.ToInt32(reader[6]),
                            PersonId = personId,
                            Amount = paymentAmount,
                            DatePaid = (DateTime) reader[8]
                        });
                    }

                    billOverview.AmountPaid += paymentAmount;

                    if (bills.Any(x => x.Id == billId) == false)
                        bills.Add(billOverview);
                }

                reader.Close();
                _connection.Close();
                return bills;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception("An Error occured while getting the bills", exception);
            }
        }

        public Bill GetBasicBillDetails(int billId)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand(
                    "SELECT Bill.\"Name\", Bill.\"Amount\", Bill.\"Due\", Bill.\"RecurringType\", Payment.\"Id\", Payment.\"Amount\", Payment.\"Created\", Person.\"Id\", Person.\"FirstName\", Person.\"LastName\" " +
                    "FROM public.\"Bill\" AS Bill " +
                    "LEFT OUTER JOIN \"Payment\" AS Payment ON Payment.\"BillId\" = Bill.\"Id\" " +
                    "LEFT OUTER JOIN \"Person\" AS Person ON Person.\"Id\" = Payment.\"PersonId\" " +
                    $"WHERE Bill.\"Id\" = {billId}", _connection);
                var reader = command.ExecuteReader();

                Bill bill = null;
                while (reader.Read())
                {
                    if (bill == null)
                    {
                        bill = new Bill
                        {
                            Id = billId,
                            Name = (string) reader[0],
                            TotalAmount = Convert.ToDecimal(reader[1]),
                            FullDateDue = (DateTime) reader[2],
                            RecurringType = (RecurringType)reader[3]
                        };
                    }

                    if (reader[4] == DBNull.Value)
                        continue;

                    var amount = Convert.ToDecimal(reader[5]);
                    bill.Payments.Add(new BillPayment
                    {
                        Id = Convert.ToInt32(reader[4]),
                        Amount = amount,
                        DatePaid = (DateTime) reader[6],
                        PersonId = Convert.ToInt32(reader[7]),
                        PersonName = (string) reader[8] + " " + (string) reader[9]
                    });

                    bill.AmountPaid += amount;
                }
                reader.Close();

                command = new NpgsqlCommand("SELECT Person.\"Id\", Person.\"Image\" " +
                                            "FROM public.\"PeopleForBill\" AS PeopleForBill " +
                                            "LEFT OUTER JOIN \"Person\" AS Person ON Person.\"Id\" = PeopleForBill.\"PersonId\" " +
                                            $"WHERE PeopleForBill.\"BillId\" = {billId}", _connection);
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var personId = Convert.ToInt32(reader[0]);
                    bill.People.Add(new BillPersonDetails
                    {
                        Id = personId,
                        Paid = bill.Payments.Any(x => x.PersonId == personId),
                        ImageLink = (string)reader[1]
                    });
                }
                reader.Close();
                _connection.Close();
                return bill;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while getting the bill (ID: {billId})", exception);
            }
        }

        public int AddBill(AddBillRequest bill)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand("INSERT INTO public.\"Bill\" (\"Name\", \"Amount\", \"Due\", \"RecurringType\") " +
                                                $"VALUES ('{bill.Name}', {bill.TotalAmount}, '{bill.Due:yyyy-MM-dd}', {(int)bill.RecurringType}) " +
                                                "RETURNING \"Id\"", _connection);
                Int64 billId = -1;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    billId = (Int64)reader[0];
                }
                reader.Close();

                foreach (var peopleId in bill.PeopleIds)
                {
                    command = new NpgsqlCommand("INSERT INTO public.\"PeopleForBill\" (\"BillId\", \"PersonId\") " +
                                                $"VALUES ({billId}, {peopleId})", _connection);
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    { }
                    reader.Close();
                }
                _connection.Close();

                return Convert.ToInt32(billId);
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while adding the bill '{bill.Name}'", exception);
            }
        }

        public bool UpdateBill(UpdateBillRequestV2 billRequest)
        {
            _connection.Open();

            try
            {
                var setValues = new List<string>();

                if (billRequest.Name != null)
                    setValues.Add($"\"Name\"='{billRequest.Name}'");
                if (billRequest.TotalAmount != null)
                    setValues.Add($"\"Amount\"={billRequest.TotalAmount}");
                if (billRequest.Due != null)
                    setValues.Add($"\"Due\"='{billRequest.Due}'");
                if (billRequest.RecurringType != null)
                    setValues.Add($"\"RecurringType\"={(int)billRequest.RecurringType}");
                
                var command = new NpgsqlCommand("UPDATE public.\"Bill\" " +
                                                $"SET {string.Join(", ", setValues)} " +
                                                $"WHERE \"Id\" = {billRequest.Id} " +
                                                "RETURNING \"Id\"", _connection);
                Int64 billId = -1;
                var rowUpdated = false;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rowUpdated = true;
                    billId = (Int64)reader[0];
                }
                reader.Close();

                if (billRequest.PeopleIds == null || billRequest.PeopleIds.Count == 0)
                    return rowUpdated;

                command = new NpgsqlCommand("DELETE FROM public.\"PeopleForBill\" " +
                                            $"WHERE \"BillId\" = {billRequest.Id}", _connection);
                reader = command.ExecuteReader();
                while (reader.Read())
                { }
                reader.Close();

                foreach (var peopleId in billRequest.PeopleIds)
                {
                    command = new NpgsqlCommand("INSERT INTO public.\"PeopleForBill\" (\"BillId\", \"PersonId\") " +
                                                $"VALUES ({billId}, {peopleId})", _connection);
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    { }
                    reader.Close();
                }
                _connection.Close();

                return rowUpdated;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while updating the bill '{billRequest.Name}'", exception);
            }
        }

        public bool DeleteBill(int billId)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand("DELETE FROM public.\"Payment\" " +
                                                $"WHERE \"BillId\" = {billId}", _connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                { }
                reader.Close();

                command = new NpgsqlCommand("DELETE FROM public.\"PeopleForBill\" " +
                                           $"WHERE \"BillId\" = {billId}", _connection);
                reader = command.ExecuteReader();
                while (reader.Read())
                { }
                reader.Close();

                command = new NpgsqlCommand("DELETE FROM public.\"Bill\" " +
                                                $"WHERE \"Id\" = {billId} " +
                                                "RETURNING \"Id\"", _connection);

                var billDeleted = false;
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    billDeleted = true;
                }
                reader.Close();

                _connection.Close();
                return billDeleted;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while deleting the bill (ID: {billId})", exception);
            }
        }

        public BillPayment GetPayment(int paymentId)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand("SELECT Payment.\"Amount\", Payment.\"Created\", Person.\"FirstName\", Person.\"LastName\" " +
                                                "FROM public.\"Payment\" AS Payment " +
                                                "LEFT OUTER JOIN public.\"Person\" AS Person ON Person.\"Id\" = Payment.\"PersonId\" " +
                                                $"WHERE Payment.\"Id\" = {paymentId}", _connection);
                BillPayment payment = null;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    payment = new BillPayment
                    {
                        Id = paymentId,
                        Amount = Convert.ToDecimal(reader[0]),
                        DatePaid = (DateTime)reader[1],
                        PersonName = (string)reader[2] + " " + (string)reader[3]
                    };
                }
                reader.Close();
                _connection.Close();

                return payment;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while getting the payment (ID: {paymentId})", exception);
            }
        }

        public void AddPayment(AddPaymentRequestV2 paymentRequest)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand("INSERT INTO public.\"Payment\" (\"BillId\", \"PersonId\", \"Amount\", \"Created\") " +
                                                $"VALUES ({paymentRequest.BillId}, {paymentRequest.PersonId}, {paymentRequest.Amount}, '{paymentRequest.Created:yyyy-MM-dd}')", _connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                { }
                reader.Close();
                _connection.Close();
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while adding the payment to the bill (ID: {paymentRequest.BillId})", exception);
            }
        }

        public bool UpdatePayment(UpdatePaymentRequestV2 paymentRequest)
        {
            _connection.Open();

            try
            {
                var setValues = new List<string>();

                if (paymentRequest.Amount != null)
                    setValues.Add($"\"Amount\"='{paymentRequest.Amount}'");
                if (paymentRequest.Created != null)
                    setValues.Add($"\"Created\"='{paymentRequest.Created:yyyy-MM-dd}'");

                var command = new NpgsqlCommand("UPDATE public.\"Payment\" " +
                                                $"SET {string.Join(", ", setValues)} " +
                                                $"WHERE \"Id\" = {paymentRequest.Id}", _connection);
                
                var rowUpdated = false;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rowUpdated = true;
                }
                reader.Close();

                return rowUpdated;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while updating the payment (ID: {paymentRequest.Id})", exception);
            }
        }

        public bool DeletePayment(int paymentRequestPaymentId)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand("DELETE FROM public.\"Payment\" " +
                                            $"WHERE \"Id\" = {paymentRequestPaymentId} " +
                                            "RETURNING \"Id\"", _connection);
                var paymentDeleted = false;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    paymentDeleted = true;
                }
                reader.Close();

                return paymentDeleted;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while deleting the payment (ID: {paymentRequestPaymentId})", exception);
            }
        }

        public List<Person> GetAllPeople()
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand(
                    "SELECT Person.\"Id\", Person.\"FirstName\", Person.\"LastName\", Person.\"Image\" " +
                    "FROM public.\"Person\" AS Person", _connection);
                var reader = command.ExecuteReader();

                List<Person> people = new List<Person>();
                while (reader.Read())
                {
                    people.Add(new Person
                    {
                        Id = Convert.ToInt32(reader[0]),
                        FirstName = (string)reader[1],
                        LastName = (string)reader[2],
                        Image = (string)reader[3]
                    });
                }

                reader.Close();
                _connection.Close();
                return people;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while getting the list of people", exception);
            }
        }

        public List<Person> GetPeople(List<int> peopleIds)
        {
            _connection.Open();

            try
            {
                var command = new NpgsqlCommand(
                    "SELECT Person.\"Id\", Person.\"FirstName\", Person.\"LastName\", Person.\"Image\" " +
                    "FROM public.\"Person\" AS Person " +
                    $"WHERE Person.\"Id\" IN ({string.Join(", ", peopleIds)})", _connection);
                var reader = command.ExecuteReader();

                List<Person> people = new List<Person>();
                while (reader.Read())
                {
                    people.Add(new Person
                    {
                        Id = Convert.ToInt32(reader[0]),
                        FirstName = (string)reader[1],
                        LastName = (string)reader[2],
                        Image = (string)reader[3]
                    });
                }

                reader.Close();
                _connection.Close();
                return people;
            }
            catch (Exception exception)
            {
                _connection.Close();
                throw new Exception($"An Error occured while getting the list of people", exception);
            }
        }
    }

    public class AddBillRequest
    {
        public string Name { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Due { get; set; }
        public List<int> PeopleIds { get; set; }
        public RecurringType RecurringType { get; set; }

        public AddBillRequest()
        {
            PeopleIds = new List<int>();
        }
    }

    public class UpdateBillRequestV2
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? Due { get; set; }
        public List<int> PeopleIds { get; set; }
        public RecurringType? RecurringType { get; set; }

        public UpdateBillRequestV2()
        {
            PeopleIds = new List<int>();
        }
    }

    public class AddPaymentRequestV2
    {
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Created { get; set; }
        public int PersonId { get; set; }
    }

    public class UpdatePaymentRequestV2
    {
        public int Id { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Created { get; set; }
    }
}