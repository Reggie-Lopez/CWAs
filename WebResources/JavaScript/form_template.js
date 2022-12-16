// JavaScript source code

// Global context for html functions
var _executionContext;
var _Xrm;

var setClientApiContext = (Xrm, executionContext) => {
    _Xrm = Xrm;
    _executionContext = executionContext;
}


function OnLoad(executionContext) {
    console.log('loaded');

    var formContext = executionContext.getFormContext();

    // Dynamic link of all callbacks
    formContext.getAttribute('mcsc_field').addOnChange(FieldOnChange)

    // List of all html files that reference this script
    var htmlWebResources = [
        'WebResource_template'
    ]

    // Instantiation of global context for html functions
    htmlWebResources.forEach((wr) => {
        control = formContext.getControl(wr);
        if (control) {
            control.getContentWindow().then((window) => {
                window.setClientApiContext(Xrm, executionContext);
            });
        }
    });
}

// Example callback
function FieldOnChange(executionContext) {

    var formContext = executionContext.getFormContext();
}

// Example html function
function ButtonClicked() {

    var formContext = _executionContext.getFormContext()
}