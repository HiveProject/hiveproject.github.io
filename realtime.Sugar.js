var clientId = '763227956228-btgo7elk6eclu3b3fho6sutsi52884io.apps.googleusercontent.com';
var model;
var openModelViewer;
var authCallback;

function loadScript(url, callback) {
	var script = document.createElement("script")
    script.type = "text/javascript";

    if (script.readyState) {  //IE
        script.onreadystatechange = function() {
            if (script.readyState == "loaded" ||
                    script.readyState == "complete") {
                script.onreadystatechange = null;
                callback();
            }
        };
    } else {  //Others
        script.onload = function(){
            callback();
        };
    }

    script.src = url;
    document.getElementsByTagName("head")[0].appendChild(script);
}

loadScript("https://apis.google.com/js/api.js", function() {
	loadScript("https://www.gstatic.com/realtime/realtime-client-utils.js", Initialize);	
});

function Initialize() {
	if (!/^([0-9])$/.test(clientId[0])) {
		alert('Invalid Client ID - did you forget to insert your application Client ID?');
	}
	realtimeUtils = new utils.RealtimeUtils({ clientId: clientId });
	authCallback();
}

// Create a new instance of the realtime utility with your client ID.
var realtimeUtils;

var loadedCallback = undefined;
function start(callback) {
	openModelViewer = gapi.drive.realtime.debug;
	// With auth taken care of, load a file, or create one if there
	// is not an id in the URL.
	var id = '0B7ZKBc-ke8aocW1ZYXJLdGpXdUE';
	loadedCallback = callback;
	if (id) {
		// Load the document id from the URL
		realtimeUtils.load(id.replace('/', ''), onFileLoaded, onFileInitialize);
	} else {
		// Create a new document, add it to the URL
		realtimeUtils.createRealtimeFile('Hive image', function(createResponse) {
			window.history.pushState(null, null, '?id=' + createResponse.id);
			realtimeUtils.load(createResponse.id, onFileLoaded, onFileInitialize);
		});
	}
}
 
// The first time a file is opened, it must be initialized with the
// document structure. This function will add a collaborative string
// to our model at the root.

 //this should only happen if we re-create the file.
function onFileInitialize(readModel) {
	model = readModel;
	var string = readModel.createString();
	string.setText('Welcome to the Quickstart App!');
	readModel.getRoot().set('demo_string', string);
	if (loadedCallback != undefined) { 
		loadedCallback(model);
	}
}

// After a file has been initialized and loaded, we can access the
// document. We will wire up the data model to the UI.
function onFileLoaded(doc) {
	model = doc.getModel();
	if (loadedCallback != undefined) {
		loadedCallback(model);
	}
} 