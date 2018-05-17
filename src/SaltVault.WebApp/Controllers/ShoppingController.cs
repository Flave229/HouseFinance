﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SaltVault.Core;
using SaltVault.Core.People;
using SaltVault.Core.Shopping;
using SaltVault.Core.Shopping.Models;
using SaltVault.WebApp.Models;

namespace SaltVault.WebApp.Controllers
{
    public class ShoppingController : Controller
    {
        private readonly IShoppingRepository _shoppingRepository;
        private readonly IPeopleRepository _peopleRepository;

        public ShoppingController(IShoppingRepository shoppingRepository, IPeopleRepository peopleRepository)
        {
            _shoppingRepository = shoppingRepository;
            _peopleRepository = peopleRepository;
        }

        public ActionResult Index()
        {
            var shoppingList = _shoppingRepository.GetAllItems(new Pagination
            {
                Page = 0,
                ResultsPerPage = int.MaxValue
            });

            return View(shoppingList);
        }

        public IActionResult AddItem()
        {
            var people = _peopleRepository.GetAllPeople();
            var shoppingModel = new ShoppingItemFormModel
            {
                Item = new ShoppingItem(),
                SelectedPeople = people.Select(x => new PersonForItem
                {
                    Person = x,
                    Selected = true
                }).ToList()
            };
            return View(shoppingModel);
        }

        [HttpPost]
        public ActionResult AddItem(ShoppingItemFormModel itemForm)
        {
            foreach (var person in itemForm.SelectedPeople)
            {
                if (person.Selected)
                    itemForm.Item.ItemFor.Add(person.Person.Id);
            }
            
            ShoppingValidator.CheckIfValidItem(itemForm.Item);
            _shoppingRepository.AddItem(new AddShoppingItemRequest
            {
                ItemFor = itemForm.SelectedPeople.Where(x => x.Selected).Select(x => x.Person.Id).ToList(),
                Name = itemForm.Item.Name,
                AddedBy = itemForm.Item.AddedBy,
                Added = DateTime.Now
            });

            return RedirectToActionPermanent("Index", "Shopping");
        }

        public IActionResult CompleteItem(int itemId)
        {
            _shoppingRepository.UpdateItem(new UpdateShoppingItemRequest
            {
                Id = itemId,
                Purchased = true
            });

            return RedirectToActionPermanent("Index", "Shopping");
        }

        public IActionResult DeleteItem(int itemId)
        {
            _shoppingRepository.DeleteItem(itemId);

            return RedirectToActionPermanent("Index", "Shopping");
        }
    }
}