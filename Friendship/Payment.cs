using System;
using System.Collections.Generic;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using System.Configuration;

namespace Friendship
{
    static class Payment
    {
        static readonly BillPaymentsClient client = BillPaymentsClientFactory.Create(Settings.Read().QiwiToken);

        public static string AddTransaction(int sum, User user, ref string billId)
        {
            try
            {
                var response = client.CreateBill(
                    info: new CreateBillInfo
                    {
                        BillId = Guid.NewGuid().ToString(),
                        Amount = new MoneyAmount
                        {
                            ValueDecimal = sum,
                            CurrencyEnum = CurrencyEnum.Rub
                        },
                        ExpirationDateTime = DateTime.Now.AddDays(5),
                        Customer = new Customer
                        {
                            Account = user.Id.ToString()
                        }
                    });
                billId = response.BillId;
                return response.PayUrl.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static bool CheckPay(User user, string billId)
        {
            try
            {
                using DB db = new DB();
                var response = client.GetBillInfo(billId);
                if (response.Status.ValueEnum == BillStatusEnum.Paid)
                {
                    user.IsDonate = true;
                    db.Update(user);
                    db.SaveChanges();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
