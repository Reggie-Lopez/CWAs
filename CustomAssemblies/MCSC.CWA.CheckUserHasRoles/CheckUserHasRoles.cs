using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.CWA.CheckUserHasRoles
{
    public class CheckUserHasRoles : CodeActivity
    {
        [Input("List of Role Names (comma-delimited)")]
        [RequiredArgument]
        public InArgument<string> RoleNames { get; set; }

        [Input("Require All Roles")]
        public InArgument<bool> RequireAll { get; set; }

        [Output("User Has Roles")]
        public OutArgument<bool> UserHasRoles { get; set; }

        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;
        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var service = executionContext.GetExtension<IOrganizationServiceFactory>()
                .CreateOrganizationService(context.UserId);

            try
            {
                var userId = context.UserId;
                var roleNames = RoleNames.Get(executionContext) ?? "";
                var requireAll = RequireAll.Get(executionContext);

                if (string.IsNullOrEmpty(roleNames))
                {
                    UserHasRoles.Set(executionContext, true);
                    return;
                }

                var names = roleNames.Split(',');

                var userQuery = new QueryExpression("role")
                {
                    ColumnSet = new ColumnSet("name"),
                    LinkEntities =
                {
                    new LinkEntity("role", "systemuserroles", "roleid", "roleid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression(LogicalOperator.And)
                        {
                            Conditions =
                            {
                                new ConditionExpression("systemuserid", ConditionOperator.Equal, userId),
                            }
                        }
                    }
                },
                    Criteria = new FilterExpression(LogicalOperator.Or)
                };

                var teamsQuery = new QueryExpression("role")
                {
                    ColumnSet = new ColumnSet("name"),
                    LinkEntities =
                {
                    new LinkEntity("role", "teamroles", "roleid", "roleid", JoinOperator.Inner)
                    {
                        LinkEntities =
                        {
                            new LinkEntity("teamroles", "teammembership", "teamid", "teamid", JoinOperator.Inner)
                            {
                                LinkCriteria = new FilterExpression(LogicalOperator.And)
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("systemuserid", ConditionOperator.Equal, userId),
                                    }
                                }
                            }
                        }
                    }
                },
                    Criteria = new FilterExpression(LogicalOperator.Or)
                };

                foreach (var name in names)
                {
                    userQuery.Criteria.AddCondition("name", ConditionOperator.Equal, name);
                    teamsQuery.Criteria.AddCondition("name", ConditionOperator.Equal, name);
                }

                var userRoles = service.RetrieveMultiple(userQuery)?.Entities?.Select(x => x.GetAttributeValue<string>("name"))?.ToList()
                    ?? new List<string>();

                var teamRoles = service.RetrieveMultiple(teamsQuery)?.Entities?.Select(x => x.GetAttributeValue<string>("name"))?.ToList()
                    ?? new List<string>();

                var roles = new List<string>(userRoles);
                roles.AddRange(teamRoles);
                roles = roles.Distinct().ToList();

                var roleCount = roles.Count;

                if (requireAll)
                {
                    UserHasRoles.Set(executionContext, roleCount = roleNames.Length);
                }
                else
                {
                    UserHasRoles.Set(executionContext, roleCount > 0);
                }

                return;
            }
            catch (Exception ex)
            {
                service.Create(new Entity("som_logentry")
                {
                    ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ["som_name"] = ex.Message,
                    ["som_details"] = ex.StackTrace,
                    ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                    ["som_recordlogicalname"] = $"systemuser",
                    ["som_recordid"] = $"{context?.UserId}",
                });

                return;
            }
        }
    }
}
