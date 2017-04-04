using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace Orleans.Providers.Firebase.Membership
{
    // TODO: Implement reminders.
    public class FirebaseReminderTable : IReminderTable
    {
        public Task Init(GlobalConfiguration config, Logger logger)
        {
            return TaskDone.Done;
        }

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            return Task.FromResult(new ReminderEntry());
        }

        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            return Task.FromResult(new ReminderTableData());
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            return Task.FromResult(new ReminderTableData());
        }

        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            return Task.FromResult(true);
        }

        public Task TestOnlyClearTable()
        {
            return TaskDone.Done;
        }

        public Task<string> UpsertRow(ReminderEntry entry)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
