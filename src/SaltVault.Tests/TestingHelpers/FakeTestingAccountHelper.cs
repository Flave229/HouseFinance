﻿using System;
using SaltVault.Core.Users;

namespace SaltVault.Tests.TestingHelpers
{
    public class FakeTestingAccountHelper
    {
        public Guid GenerateValidFakeCredentials()
        {
            return UserCache.StaticGenerateUserSession(new ActiveUser
            {
                PersonId = 5,
                HouseId = 3
            });
        }

        public void CleanUp(Guid? sessionId)
        {
            if (sessionId != null)
                UserCache.TerminateSession((Guid)sessionId);
            // Code to delete all the bills, payments, shopping items and to do tasks associated with the test user
        }
    }
}
