function AddCustomFilter(executionContext) {
    var formContext = executionContext.getFormContext();
  formContext.getAttribute('som_leavetype').addOnChange(AddCustomFilter);
    //Check if the field is not null
    if (formContext.getAttribute("som_leavetype").getValue() != null) {
        //Apply Search on lookup field OnClick 
        formContext.getControl("som_leavedenialreason1").addPreSearch(function () {
            var leaveType = formContext.getAttribute("som_leavetype").getValue();
            //Apply the filter condition
            var filter = "<filter type='and'><condition attribute='som_leavetype' operator='eq' value='" + leaveType + "' /></filter>";
            //Populate the filter values into lookup field
            formContext.getControl("som_leavedenialreason1").addCustomFilter(filter);
             });

        //Apply Search on lookup field OnClick 
        formContext.getControl("som_leavedenialreason2").addPreSearch(function () {
            var leaveType = formContext.getAttribute("som_leavetype").getValue();
            //Apply the filter condition
            var filter = "<filter type='and'><condition attribute='som_leavetype' operator='eq' value='" + leaveType + "' /></filter>";
            //Populate the filter values into lookup field
            formContext.getControl("som_leavedenialreason2").addCustomFilter(filter);
        });

    }
}