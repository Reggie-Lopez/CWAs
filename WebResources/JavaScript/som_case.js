function case_OnLoad(context) {
    console.log('form onload called');

    //run form logic
    CheckCorrectForm(context);   
}


function case_OnSave(context) {
    console.log('form onsave called');
    var formContext = context.getFormContext();
}


function CheckCorrectForm(context) {
    //get current form
    var formContext = context.getFormContext();
    var formSaveType = formContext.ui.getFormType();
    //get current form
    //var formItem = formContext.ui.formSelector.getCurrentItem();

    //get case type lookup 
    var caseType = formContext.getAttribute("som_casetype");
    if (caseType != null) {
        var caseTypeLookup = caseType.getValue();
        var caseTypeLookupName = caseTypeLookup[0].name;

        //check if the record has been saved and it's STILL on the entry form. redirect to correct form
        if (caseTypeLookupName == "Entry" && formSaveType != 1) {
            ChangeForm(formContext);
        }

        //check if the current form is NOT Entry and it's a new record
        if (caseTypeLookupName != "Entry" && formSaveType == 1) {
            ChangeForm(formContext, true);
        }
    }
}


function ChangeForm(formContext, changeToEntryForm) {
    //get casetype optionset
    //var caseType = formContext.getAttribute("casetypecode");

    //get case type lookup 
    var caseType = formContext.getAttribute("som_casetype");
    if (caseType != null) {
        var caseTypeLookup = caseType.getValue();
        //get text of lookup
        var caseTypeLookupName = caseTypeLookup[0].name;
        //override if the ChangeToEntryForm is true
        if (changeToEntryForm) { caseTypeLookupName = "Entry"; }
             
        //get all forms
        var allForms = formContext.ui.formSelector.items.get();
        //loop through all forms and set to the form that was selected in the casetype optionset
        var newForm;
        for (var f in allForms) {
            var form = allForms[f];
            var formName = form.getLabel().toLowerCase();
            //check if the form is the same as the option selected in the optionset
            if (formName == caseTypeLookupName) {
                //set var and exit loop
                newForm = form;
                break;
            }
        }
        if (!newForm) {
            console.log("No form created for the case type record selected.");
            alert("No form created for the case type record selected.");
            return;
        }

        //change form
        newForm.navigate();
        return true;                  
    }
}