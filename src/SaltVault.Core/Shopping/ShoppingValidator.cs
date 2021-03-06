﻿using System;
using SaltVault.Core.Shopping.Models;

namespace SaltVault.Core.Shopping
{
    public class ShoppingValidator
    {
        public static void CheckIfValidItem(ShoppingItem item)
        {
            try
            {
                if (item == null) throw new System.Exception("The shopping item object given was null.");
                if (item.AddedBy <= 0) throw new System.Exception("The person creating the shopping item must be defined");
                if (item.ItemFor.Count <= 0) throw new System.Exception("The shopping item must be created for at least one person");
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("The shopping item object cannot be validated: " + ex.Message, ex);
            }
        }

        public static void CheckIfValidItem(UpdateShoppingItemRequest item)
        {
            try
            {
                if (item == null) throw new System.Exception("The shopping item object given was null.");
                if (item.ItemFor != null && item.ItemFor.Count <= 0) throw new System.Exception("The shopping item must be created for at least one person");
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("The shopping item object cannot be validated: " + ex.Message, ex);
            }
        }
    }
}
