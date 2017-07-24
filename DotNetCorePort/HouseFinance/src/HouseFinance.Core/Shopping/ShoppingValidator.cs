﻿using System;

namespace HouseFinance.Core.Shopping
{
    public class ShoppingValidator
    {
        public static void CheckIfValidItem(ShoppingItem item)
        {
            try
            {
                if (item == null) throw new Exception("The shopping item object given was null.");
                if (item.AddedBy == new Guid()) throw new Exception("The person creating the shopping item must be defined");
                if (item.ItemFor.Count <= 0) throw new Exception("The shopping item must be created for at least one person");
            }
            catch (Exception ex)
            {
                throw new Exception("The shopping item object cannot be validated: " + ex.Message, ex);
            }
        }
    }
}