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
    var formItem = formContext.ui.formSelector.getCurrentItem();

    //check if the record has been saved and it's STILL on the entry form. redirect to correct form
    if (formItem.getLabel() == "Entry" && formSaveType != 1) {
        ChangeForm(formContext);
    }

    //check if the current form is NOT Entry and it's a new record
    if (formItem.getLabel() != "Entry" && formSaveType == 1) {
        ChangeForm(formContext, true);
    }

}


function ChangeForm(formContext, changeToEntryForm) {
    //get casetype optionset
    var caseType = formContext.getAttribute("casetypecode");

    if (caseType != null) {
        //get text value of optionset
        var formNameToChangeTo = caseType.getText();
        //override if the ChangeToEntryForm is true
        if (changeToEntryForm) { formNameToChangeTo = "Entry"; }
             
        //get all forms
        var allForms = formContext.ui.formSelector.items.get();
        //loop through all forms and set to the form that was selected in the casetype optionset
        var newForm;
        for (var f in allForms) {
            var form = allForms[f];
            var formName = form.getLabel().toLowerCase();
            //check if the form is the same as the option selected in the optionset
            if (formName == formNameToChangeTo.toLowerCase()) {
                //set var and exit loop
                newForm = form;
                break;
            }
        }
        if (!newForm) {
            console.log("No form created for the optionset value selected.");
            alert("No form created for the optionset value selected.");
            return;
        }

        //change form
        newForm.navigate();
        return true;                  
    }
}