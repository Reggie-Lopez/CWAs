function transaction_OnLoad(executionContext) {
    console.log('form onload called');

    var formContext = executionContext.getFormContext();

    formContext.getAttribute('som_secondarycategory').addOnChange(secondarycategory_OnChange);

    //default Position field
    setPosition(executionContext);
}


function transaction_OnSave(context) {
    console.log('form onsave called');
    var formContext = context.getFormContext();

    
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