﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SaltVault.Core.Exception;
using SaltVault.IntegrationTests.TestingHelpers;
using SaltVault.WebApp.Models.Bills;

namespace SaltVault.IntegrationTests.Authentication.GivenASessionId
{
    [TestClass]
    public class WhenTheSessionIdHasExpired
    {
        private Guid _expiredSessionId;
        private FakeTestingAccountHelper _fakeTestingAccountHelper;
        private EndpointHelper _endpointHelper;

        [TestInitialize]
        public void Initialize()
        {
            _fakeTestingAccountHelper = new FakeTestingAccountHelper();
            _expiredSessionId = _fakeTestingAccountHelper.GenerateValidExpiredFakeCredentials();
            _endpointHelper = new EndpointHelper();
            _endpointHelper.Setup().SetAuthenticationToken(_expiredSessionId.ToString());
        }

        [TestMethod]
        public void ThenTheResponseContainsAnErrorWithExpiredCode()
        {
            string responseContent = _endpointHelper.GetBills();
            GetBillListResponse billListResponse = JsonConvert.DeserializeObject<GetBillListResponse>(responseContent);

            Assert.IsTrue(billListResponse.HasError);
            Assert.IsTrue(billListResponse.Error.ErrorCode == ErrorCode.USER_SESSION_EXPIRED);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _fakeTestingAccountHelper.CleanUp(_expiredSessionId);
        }
    }
}