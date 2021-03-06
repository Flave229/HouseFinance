﻿using System;
using SaltVault.Core.Bills.Models;
using SaltVault.Core.Validation;

namespace SaltVault.Core.Bills.Payments
{
    public class PaymentValidator
    {
        private static readonly ValidationService Validation = new ValidationService();

        public static void CheckAmountValid(decimal amount)
        {
            var minAmount = 0.01m;
            var maxAmount = 1000000;

            if (!Validation.CheckDecimalWithinSizeRange(minAmount, maxAmount, amount))
                throw new System.Exception("The amount entered for the payment was out of range. Value must lie between " + minAmount + " and " + maxAmount + ".");
        }

        public static void CheckDateValid(DateTime date)
        {
            var minAmount = new DateTime(1970, 1, 1);
            var maxAmount = DateTime.Now;

            if (!Validation.CheckDateWithinRange(minAmount, maxAmount, date))
                throw new System.Exception("The date entered for the payment was out of range. Value must lie between " + minAmount.ToString("d") + " and " + maxAmount.ToString("d") + ".");
        }

        public static void CheckIfValidPayment(Payment payment, int billId)
        {
            try
            {
                if (payment == null) throw new System.Exception("The payment object given was null.");
                if (payment.PersonId <= 0) throw new System.Exception("The person id given was invalid.");
                if (billId <= 0) throw new System.Exception("The bill id given was invalid.");

                CheckAmountValid(payment.Amount);
                CheckDateValid(payment.DatePaid);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("The payment object cannot be validated: " + ex.Message, ex);
            }
        }

        public static void CheckIfValidPayment(UpdatePaymentRequest payment)
        {
            try
            {
                if (payment == null) throw new System.Exception("The payment object given was null.");
                if (payment.Id <= 0) throw new System.Exception("The payment id given was invalid.");

                if (payment.Amount != null)
                    CheckAmountValid((decimal)payment.Amount);

                if (payment.Created != null)
                    CheckDateValid((DateTime)payment.Created);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("The payment object cannot be validated: " + ex.Message, ex);
            }
        }
    }
}
