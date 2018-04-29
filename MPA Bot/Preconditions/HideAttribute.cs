using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace MPA_Bot.Preconditions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HideAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider service)
        {
            var mContext = context as MPAContext;

            if (mContext == null || !mContext.IsHelp)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("Help command, cannot run"));
        }
    }
}
