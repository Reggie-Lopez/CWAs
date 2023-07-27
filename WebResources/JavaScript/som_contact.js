function contact_OnLoad(context) {
    console.log('form onload called');
    //run form logic
    ChangeToCorrectForm(context);   
}




function ChangeToCorrectForm(context) {
    //get current form
    var formContext = context.getFormContext();

    //get current form
    var formSaveType = formContext.ui.getFormType();    
    var formItem = formContext.ui.formSelector.getCurrentItem();   

    var formToChangeTo;
    var contactType = formContext.getAttribute("som_contacttype").getText();

    if (contactType == 'Dependent') {
             if (formItem.getLabel() != "Dependent") {
            formToChangeTo = GetForm(context, "Dependent");
        }
    } else {
        if (formItem.getLabel() != "MCSC") {
            formToChangeTo = GetForm(context, "MCSC");
        }
    }


    if (formToChangeTo) {
        //change form
        formToChangeTo.navigate();
        return true;
    }    
}


function GetForm(context, formOverrride) {
 var formContext = context.getFormContext();
    //get all forms
    var allForms = formContext.ui.formSelector.items.get();
    //loop through all forms and set to the form that was selected in the contacttype lookup
    for (var f in allForms) {
        var form = allForms[f];
        var formName = form.getLabel().toLowerCase();
        //check if the form is the same as the value in the lookup
        if (formName == formOverrride.toLowerCase()) {
            //return form
            return form;
        }
    }
    
    //if it can't find the form then return null
    return null;
}