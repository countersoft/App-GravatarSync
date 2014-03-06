using System;
using System.Linq;
using Countersoft.Gemini;
using Countersoft.Gemini.Commons;
using Countersoft.Foundation.Commons.Extensions;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Contracts.Business;
using Countersoft.Gemini.Infrastructure.Managers;
using Countersoft.Gemini.Infrastructure.TimerJobs;
using Countersoft.Gemini.Infrastructure.Helpers;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Extensibility.Apps;

namespace GravatarSync
{
    [AppType(AppTypeEnum.Timer), 
    AppGuid("9DCCB6BF-9B84-4127-B9FD-644563FA6854"), 
    AppName("Gravatar Sync"), 
    AppDescription("Keeps Gravatar images in sync with Gravatar.com")]
    public class GravatarSync : TimerJob 
    {
        public override bool Run(IssueManager issueManager)
        {
            var userManager = new UserManager(issueManager);

            var activeUsers = userManager.GetActiveUsers();

            foreach (UserDto user in activeUsers)
            {
                if (user.Entity.Id == Constants.AnonymousUserId) continue;

                try
                {
                    UserSettings settings = user.GetSettings();

                    if (settings.UseGravatar)
                    {
                        byte[] picture = ImageHelper.GetGravatar(user.Entity.Email.ToLowerInvariant(), 64);

                        if (picture != null && (settings.Picture == null || !picture.SequenceEqual(settings.Picture)))
                        {
                            settings.Picture = picture;

                            userManager.UpdateSettings(user.Entity.Id, settings);

                            LogDebugMessage(string.Concat("Gravatar was updated for ", user.Entity.Email));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            return true;
        }

        public override void Shutdown()
        {
            // app logic for shutdown event
        }

        public override TimerJobSchedule GetInterval(IGlobalConfigurationWidgetStore dataStore)
        {
            var data = dataStore.Get<TimerJobSchedule>(AppGuid);

            if (data == null || data.Value == null || (data.Value.Cron.IsEmpty() 
                && data.Value.IntervalInHours.GetValueOrDefault() == 0 
                && data.Value.IntervalInMinutes.GetValueOrDefault() == 0))
            {
                // Interval 60 minutes is default
                return new TimerJobSchedule(60);
            }

            return data.Value;
        }
    }
}
