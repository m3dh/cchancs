namespace ChatChan.Provider.StoreModel
{
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;

    public class UserAccount : ISqlRecord
    {
        public Task Fill(DbDataReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}
