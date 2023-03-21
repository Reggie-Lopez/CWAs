function case_OnLoad(context) {
    console.log('form onload called');

    //run form logic
    ChangeToCorrectForm(context);   
}


function case_OnSave(context) {
    console.log('form onsave called');
    var formContext = context.getFormContext();

    //confirm there is an actual form named what is in the Case Type lookup
    var formCheck = GetForm(context);
    //if no form is returned then add notification to record
    if (!formCheck) {
        var errorMsg = "Form does not exist for the Case Type selected.";
        formContext.ui.setFormNotification(errorMsg, "ERROR", "NoFormExists");
        context.getEventArgs().preventDefault();
        console.log(errorMsg);
    }
}


function ChangeToCorrectForm(context) {
    //get current form
    var formContext = context.getFormContext();

    //get current form
    var formSaveType = formContext.ui.getFormType();    
    var formItem = formContext.ui.formSelector.getCurrentItem();   

    var formToChangeTo;
    var caseType = formContext.getAttribute("som_casetype").getValue();

    //if the current form is Entry but the record is already saved then change to appropriate form
    if (formItem.getLabel() == "Entry" && caseType != null && formSaveType != 1) {
        formToChangeTo = GetForm(context);
    }

    //if the case type is empty but it happens to already have been created
    if (formItem.getLabel() != "Entry" && caseType == null && formSaveType != 1) {
        formToChangeTo = GetForm(context, "Entry");
    }

    //if the current form is NOT Entry and the record has not been saved yet
    if (formItem.getLabel() != "Entry" && formSaveType == 1) {
        formToChangeTo = GetForm(context, "Entry");
    }

    if (formToChangeTo) {
        //change form
        formToChangeTo.navigate();
        return true;
    }    
}


function GetForm(context, formOverrride) {
    var formContext = context.getFormContext();

    //override if the ChangeToEntryForm is true
    if (formOverrride) { caseTypeLookupName = "Entry"; }

    //get case type lookup 
    var caseType = formContext.getAttribute("som_casetype").getValue();
    if (caseType != null) {
        //get case type lookup name
        caseTypeLookupName = caseType[0].name;
    }

    //get all forms
    var allForms = formContext.ui.formSelector.items.get();
    //loop through all forms and set to the form that was selected in the casetype lookup
    for (var f in allForms) {
        var form = allForms[f];
        var formName = form.getLabel().toLowerCase();
        //check if the form is the same as the value in the lookup
        if (formName == caseTypeLookupName.toLowerCase()) {
            //return form
            return form;
        }
    }
    
    //if it can't find the form then return null
    return null;
}