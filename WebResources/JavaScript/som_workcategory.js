function workcategory_OnLoad(executionContext) {
    console.log('form onload called');

    var formContext = executionContext.getFormContext();

    formContext.getAttribute('som_secondarycategory').addOnChange(secondarycategory_OnChange);

    setPosition(executionContext);
}

function secondarycategory_OnChange(executionContext) {
    console.log('secondary category onchange called');

    var formContext = executionContext.getFormContext();

    var secondaryCategory = formContext.getAttribute('som_secondarycategory').getValue()?.[0]?.id;

    if (!secondaryCategory) {
        return;
    }

    Xrm.WebApi.retrieveRecord('som_secondarycategory', secondaryCategory, '?$select=som_description').then(result => {
        if (!result.som_description) {
            return;
        }
        
        formContext.getAttribute('som_description').setValue(result.som_description);
    }, error => {
        console.log(error);
    });
}

function setPosition(executionContext) {

    var formContext = executionContext.getFormContext();

    var position = formContext.getAttribute('som_position').getValue()?.[0]?.id;
    if (!!position) {
        return;
    }

    Xrm.WebApi.retrieveRecord('systemuser', formContext.context.getUserId(), '?$expand=positionid($select=positionid)').then(result => {
        var positionid = [
            {
                id: result['_positionid_value'],
                name: result["_positionid_value@OData.Community.Display.V1.FormattedValue"],
                entityType: result["_positionid_value@Microsoft.Dynamics.CRM.lookuplogicalname"]
            }
        ];
        
        formContext.getAttribute('som_position').setValue(positionid);
    }, error => {
        console.log(error);
    });
}