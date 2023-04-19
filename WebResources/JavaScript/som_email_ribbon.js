function ConvertToTransaction_Action(primaryControl, selectedControlSelectedItemIds) {
    console.log('loaded');

    var formContext = primaryControl;

    var selectedRows = formContext.getControl('attachmentsGrid')?.getGrid()?.getSelectedRows();

    attachments = []
    selectedRows.forEach(row => attachments.push(row?.getData()?.getEntity()?.getId()?.replace("{", "")?.replace("}", "")));

    var title = "Convert Email to Transaction";

    if (!attachments.length) {

        var alertStrings = {
            title: title,
            text: "You must select at least one attachment.",
            confirmButtonLabel: "OK"
        };

        var alertOptions = { height: 200, width: 500 };

        Xrm.Navigation.openAlertDialog(alertStrings, alertOptions).then(success => {

        }, error => {
            console.log(error.message);
        });

        return;
    }

    var csvAttachments = attachments.join();

    var transaction = formContext.getAttribute('som_transaction')?.getValue()?.[0] ?? {};

    var text = `Are you sure you'd like to add ${attachments.length} selected files into a new transaction?`
    
    if (transaction.name) {
        text = `Are you sure you'd like to add ${attachments.length} selected files into the transaction ${transaction.name}?`
    }

    var confirmStrings = {
        title: title,
        text: text,
        confirmButtonLabel: "Yes",
        cancelButtonLabel: "No"
    };

    var confirmOptions = { height: 200, width: 500 };
    Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions).then(success => {
        if (!success.confirmed) {
            return;
        }

        CallConvertAction(formContext, transaction, csvAttachments);
    });
}

function CallConvertAction(formContext, transaction, csvAttachments) {

    var emailId = formContext.data.entity.getId().replace('{', '').replace('}', '');
    var transactionId = transaction?.id?.replace('{', '')?.replace('}', '');

    var req = {
        entity: {
            entityType: "email",
            id: emailId
        },
        AttachmentIdList: csvAttachments,
        ExistingTransactionId: transactionId,
        getMetadata: () => {
            return {
                boundParameter: "entity",
                parameterTypes: {
                    entity: {
                        typeName: "mscrm.email",
                        structuralProperty: 5
                    },
                    AttachmentIdList: {
                        typeName: "Edm.String",
                        structuralProperty: 1
                    },
                    ExistingTransactionId: {
                        typeName: "Edm.String",
                        structuralProperty: 1
                    }
                },
                operationType: 0,
                operationName: "som_ConvertEmailToTransaction"
            }
        }
    };

    Xrm.WebApi.online.execute(req).then(result => {
        result.json().then(response => {
            var pageInput = {
                pageType: "entityrecord",
                entityName: "som_transaction",
                entityId: response.RedirectTransactionId
            };

            Xrm.Navigation.navigateTo(pageInput).then(() => {}, () => {});
        });
    }, error => {
        console.log(error.message);
    });
}

function ConvertToTransaction_Enable(primaryControl) {
    console.log('loaded');

    var formContext = primaryControl;
}