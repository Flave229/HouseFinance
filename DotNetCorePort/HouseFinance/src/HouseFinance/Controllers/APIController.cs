﻿using System;
using System.Collections.Generic;
using HouseFinance.Core.Authentication;
using HouseFinance.Core.Bills;
using HouseFinance.Core.Bills.Payments;
using HouseFinance.Core.FileManagement;
using HouseFinance.Core.Shopping;
using HouseFinance.Models.API;
using HouseFinance.Models.Bills;
using HouseFinance.Models.Shopping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace HouseFinance.Controllers
{
    public class ApiController : Controller
    {
        [HttpGet]
        [Route("Api/Bills")]
        public GetBillListResponse GetBillList()
        {
            var response = new GetBillListResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                response.Bills = BillListBuilder.BuildBillList();
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        [HttpPost]
        [Route("Api/Bills")]
        public GetBillResponse GetBill([FromBody]GetBillRequest billRequest)
        {
            var response = new GetBillResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                response.Bill = BillDetailsBuilder.BuildBillDetails(Guid.Parse(billRequest.BillId));
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        [HttpPost]
        [Route("Api/Bills/Add")]
        public CommunicationResponse AddBill([FromBody]Bill billRequest)
        {
            var response = new CommunicationResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                BillValidator.CheckIfValidBill(billRequest);
                var genericFileHelper = new GenericFileHelper(FilePath.Bills);
                genericFileHelper.AddOrUpdate<Bill>(billRequest);

                response.Notifications = new List<string>
                {
                    $"The bill '{billRequest.Name}' has been added"
                };
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        [HttpDelete]
        public CommunicationResponse DeleteBill([FromBody]DeleteBillRequest deleteBillRequest)
        {
            var response = new CommunicationResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                var genericFileHelper = new GenericFileHelper(FilePath.Bills);
                var bill = genericFileHelper.Get<Bill>(deleteBillRequest.BillId);
                genericFileHelper.Delete<Bill>(deleteBillRequest.BillId);

                response.Notifications = new List<string>
                {
                    $"The bill '{bill.Name}' has been deleted"
                };
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        [HttpPost]
        [Route("Api/Bills/AddPayment")]
        public CommunicationResponse AddPayment([FromBody]AddPaymentRequest paymentRequest)
        {
            var response = new CommunicationResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    Amount = paymentRequest.Amount,
                    Created = paymentRequest.Created,
                    PersonId = paymentRequest.PersonId
                };

                PaymentValidator.CheckIfValidPayment(payment);
                var paymentFileHelper = new GenericFileHelper(FilePath.Payments);
                paymentFileHelper.AddOrUpdate<Payment>(payment);

                var billFileHelper = new GenericFileHelper(FilePath.Payments);
                var realBill = billFileHelper.Get<Bill>(paymentRequest.BillId);

                BillHelper.AddOrUpdatePayment(ref realBill, payment);

                billFileHelper.AddOrUpdate<Bill>(realBill);

                response.Notifications = new List<string>
                {
                    "The payment has been added"
                };
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        [HttpGet]
        [Route("Api/Shopping")]
        public GetShoppingResponse GetShoppingItems()
        {
            var response = new GetShoppingResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                response.Items = ShoppingListBuilder.BuildShoppingList();
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        [HttpPost]
        [Route("Api/Shopping/Add")]
        public CommunicationResponse AddShoppingItem([FromBody]ShoppingItem shoppingRequest)
        {
            var response = new CommunicationResponse();
            if (Authenticate(Request.Headers["Authorization"]) == false)
            {
                response.AddError("The API Key was invalid");
                return response;
            }

            try
            {
                ShoppingValidator.CheckIfValidItem(shoppingRequest);
                new GenericFileHelper(FilePath.Shopping).AddOrUpdate<ShoppingItem>(shoppingRequest);

                response.Notifications = new List<string>
                {
                    $"The shopping item '{shoppingRequest.Name}' has been added"
                };
            }
            catch (Exception exception)
            {
                response.AddError($"An unexpected exception occured: {exception}");
            }

            return response;
        }

        private bool Authenticate(StringValues authorizationHeader)
        {
            var apiKey = authorizationHeader.ToString().Replace("Token ", "");
            return Authentication.CheckKey(apiKey);
        }
    }
}