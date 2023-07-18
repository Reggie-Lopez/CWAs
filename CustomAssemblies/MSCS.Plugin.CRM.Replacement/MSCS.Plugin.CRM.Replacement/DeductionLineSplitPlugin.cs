using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MSCS.Plugin.CRM.Replacement
{
	public class DeductionLineSplitPlugin : IPlugin
	{
		public void Execute(IServiceProvider serviceProvider)
		{
			IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

			IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
			IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

			ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

			try
			{
				tracingService.Trace("Before DeductionLineSplitPlugin");
				if (context.InputParameters.Contains("SelectedDLs") && context.InputParameters["SelectedDLs"] is string)
				{
					tracingService.Trace("Started DeductionLineSplitPlugin");
					string[] selectedIds = context.InputParameters["SelectedDLs"].ToString().Split(',');
					tracingService.Trace("selectedIds Count : " + selectedIds.Count());
					foreach (string dedLineId in selectedIds)
					{
						Entity deductionEnt = service.Retrieve("som_npadeductionline", new Guid(dedLineId), new ColumnSet("som_name", "som_deductioncode", "som_oldcurrent", "som_numberofpayperiods"
							, "som_effectivedate", "som_eedeductionamount", "som_npagpa", "som_deductionoptionamount"));
						tracingService.Trace("som_eedeductionamount Retrieved : " + deductionEnt.Contains("som_eedeductionamount"));
						tracingService.Trace("som_deductionoptionamount Retrieved : " + deductionEnt.Contains("som_deductionoptionamount"));
						if (deductionEnt.Contains("som_eedeductionamount") && deductionEnt.Contains("som_deductionoptionamount"))
						{
							decimal totalAmount = deductionEnt.GetAttributeValue<Money>("som_eedeductionamount").Value;
							tracingService.Trace("totalAmount : " + totalAmount);
							EntityReference optionAmountEntRef = deductionEnt.GetAttributeValue<EntityReference>("som_deductionoptionamount");
							if (totalAmount > 100)
							{


								int splitCount = (int)(totalAmount / 100) + 1;
								if (totalAmount % 100 == 0)
									splitCount = splitCount - 1;

								tracingService.Trace("splitCount : " + splitCount);
								var tempAmount = totalAmount;
								for (int i = 0; i < splitCount; i++)
								{
									try
									{
										tracingService.Trace("tempAmount : " + tempAmount);
										tracingService.Trace("i : " + i);
										if (i == 0)
										{
											Entity updateDeductionEnt = new Entity("som_npadeductionline", deductionEnt.Id);
											updateDeductionEnt["som_effectivedate"] = DateTime.UtcNow;
											updateDeductionEnt["som_isnpadeductionsplit"] = true;
											 updateDeductionEnt["som_eetotalamountadjusted_proxy"] = new Money(100);
											service.Update(updateDeductionEnt);

											tracingService.Trace("dedOptionAmountEt/updateDeductionEnt Updated ");
											tempAmount = tempAmount - 100;
										}
										else
										{


											Entity newDeductionEnt = new Entity("som_npadeductionline");
											if (deductionEnt.Contains("som_name"))
												newDeductionEnt["som_name"] = deductionEnt["som_name"];
											if (deductionEnt.Contains("som_npagpa"))
												newDeductionEnt["som_npagpa"] = deductionEnt["som_npagpa"];
											if (deductionEnt.Contains("som_deductioncode"))
												newDeductionEnt["som_deductioncode"] = deductionEnt["som_deductioncode"];
											if (deductionEnt.Contains("som_numberofpayperiods"))
												newDeductionEnt["som_numberofpayperiods"] = deductionEnt["som_numberofpayperiods"];
											if (deductionEnt.Contains("som_oldcurrent"))
												newDeductionEnt["som_oldcurrent"] = deductionEnt["som_oldcurrent"];
											newDeductionEnt["som_effectivedate"] = DateTime.UtcNow.AddDays(14);
											newDeductionEnt["som_deductionoptionamount"] = optionAmountEntRef;
											newDeductionEnt["som_isnpadeductionsplit"] = true;
											if (tempAmount > 100)
											{
												newDeductionEnt["som_eetotalamountadjusted_proxy"] = new Money(100);
												tempAmount = tempAmount - 100;
											}
											else
												newDeductionEnt["som_eetotalamountadjusted_proxy"] = new Money(tempAmount);

											service.Create(newDeductionEnt);
											tracingService.Trace("newDeductionEnt Created");
										}
									}
									catch (Exception ex)
									{
										tracingService.Trace(ex.Message);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new InvalidPluginExecutionException("The following error occurred in MyPlugin.", ex);
			}
		}
	}
}
