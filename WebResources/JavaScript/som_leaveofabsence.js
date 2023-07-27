function leaveofabsence_OnLoad(context) {
    console.log('form onload called');
    var formContext = context.getFormContext();

    formContext.getAttribute('som_leavetype').addOnChange(AddCustomFilter);
    formContext.getAttribute('som_leavetype').addOnChange(ClearLeaveDenialReasons);

    AddCustomFilter(context);
}


function leaveofabsence_OnSave(context) {
    console.log('form onsave called');
    
}


function ClearLeaveDenialReasons(context) {
    var formContext = context.getFormContext();
    formContext.getAttribute("som_leavedenialreason1").setValue(null);
    formContext.getAttribute("som_leavedenialreason2").setValue(null);
}


function AddCustomFilter(executionContext) {
    var formContext = executionContext.getFormContext();
    formContext.getControl("som_leavedenialreason1").addPreSearch(function () {
        var leaveType = formContext.getAttribute("som_leavetype").getValue();
        //Apply the filter condition
        var filter = "<filter type='and'><condition attribute='som_leavetype' operator='eq' value='" + leaveType + "' /></filter>";
        //Populate the filter values into lookup field
        formContext.getControl("som_leavedenialreason1").addCustomFilter(filter);
    });

    formContext.getControl("som_leavedenialreason2").addPreSearch(function () {
        var leaveType = formContext.getAttribute("som_leavetype").getValue();
        //Apply the filter condition
        var filter = "<filter type='and'><condition attribute='som_leavetype' operator='eq' value='" + leaveType + "' /></filter>";
        //Populate the filter values into lookup field
        formContext.getControl("som_leavedenialreason2").addCustomFilter(filter);
    });

}