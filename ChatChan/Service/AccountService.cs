namespace ChatChan.Service
{
    using System;

    public class AccountService
    {
        [Flags]
        private enum UserAccountStatus : int
        {
            CanUpdateFields = 0x00000001,
            CanUpdatePassword = 0x00000002,
            IsNew = 0x00000004,

            NewAccount = IsNew | CanUpdatePassword | CanUpdateFields,
        }
    }
}
