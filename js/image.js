	// Your Client ID can be retrieved from your project in the Google
	// Developer Console, https://console.developers.google.com
	var CLIENT_ID = '763227956228-btgo7elk6eclu3b3fho6sutsi52884io.apps.googleusercontent.com';

	var SCOPES = ['https://www.googleapis.com/auth/drive.metadata'];
	//HiveProject folder.
	var baseFolder= '0B7Fr-eaQNfHDOU81U2VJRU5VbGc'
	var folder = baseFolder;
	var hiveMimeType='application/vnd.google-apps.drive-sdk'; //i think we can change this.
	/**
	 * Check if current user has authorized this application.
	 */
	function checkAuth() {
		gapi.auth.authorize({
			'client_id': CLIENT_ID,
			'scope': SCOPES.join(' '),
			'immediate': true
		}, handleAuthResult);
	}

	/**
	 * Handle response from authorization server.
	 *
	 * @param {Object} authResult Authorization result.
	 */
	function handleAuthResult(authResult) {
		var authorizeDiv = document.getElementById('authorize-div');
		if (authResult && !authResult.error) {
			// Hide auth UI, then load client library.
			authorizeDiv.style.display = 'none';
			loadDriveApi();
		} else {
			// Show auth UI, allowing the user to initiate authorization by
			 // clicking authorize button.
			authorizeDiv.style.display = 'inline';
		}
	}

	/**
	 * Initiate auth flow in response to user clicking authorize button.
	 *
	 * @param {Event} event Button click event.
	 */
	function handleAuthClick(event) {
		gapi.auth.authorize({
			client_id: CLIENT_ID, 
			scope: SCOPES, 
			immediate: false
		}, handleAuthResult);
		return false;
	}
	$( document ).ready(function() {
		handleAuthClick();
	});
	var openModelViewer;
	function loadDriveApi() {
		//load realtime api.
		
		gapi.load('auth:client,drive-realtime,drive-share', function(){
			gapi.client.load('drive', 'v2', function () {
				// Get query param "id"
				var param = location.search.substring(1).split("&")
					.map(function (param) { return param.split("="); })
					.find(function (param) { return param[0] === "id"; });
				openModelViewer=gapi.drive.realtime.debug;
				loadDocument(param[1]);
				
			});
		});
		
	}
	
	
	var context;
	var model;
	var doc;
	function loadDocument(documentId){
		gapi.drive.realtime.load(documentId, function(loadedDoc) {
			doc=loadedDoc;
			model=doc.getModel();
			hiveLoaded(model);
		});
	}
 	
	function hiveLoaded(model) {
		if(model.getRoot().get('context') == null) {
			initializeObjects();
		} else { 
			context = model.getRoot().get('context');
		}
		openModelViewer();
		REPL.start();
	}
				
	// startups the system
	function initializeObjects() {
	  
		context = CreateContext();
		 
		model.getRoot().set('context',context);
		
		context.set('object', model.createMap());
	 
		context.get('object').set('basicNew', CreateMethod(
			'basicNew',"(function () { var self = model.createMap();context.set('self',self); initObject(self,context);return self;})",context));
		
		compiler.evaluate("null := (object basicNew addMethod:[isNull| true]) addMethod:[toString|'null']")
		InitializeNumbers();
		InitializeStrings();
		InitializeLists();
		InitializeOthers();
	}
	
	function initObject(obj,context) {
		obj.set('addMethod:',CreateMethod(
			'addMethod:',"(function(b){context.lookup('self').get().set(b.get('selector'),b); return context.lookup('self').get();})",context));
		obj.set('toString',CreateMethod('toString',"(function(){ return CreateString('An Object');})",context));
		obj.set('Equals:',CreateMethod(
			'Equals:',"(function(other){if(context.lookup('self').get()==other){return context.lookup('true').get();}else{return context.lookup('false').get();}})",context));
		
		obj.set('isNull',CreateMethod('isNull',"(function(){ return context.lookup('false').get();})",context));		
	}
	
	function InitializeNumbers() {
		compiler.evaluate("Number := object basicNew");
		
		compiler.evaluate("Number addMethod:[value | self]");
		context.get('Number').set('+', CreateMethod(
			'+',"(function (b) {return (context.lookup('self').get() + b);})",context));
		context.get('Number').set('/', CreateMethod(
			'/',"(function (b) {return (context.lookup('self').get() / b);})",context));
		
		context.get('Number').set('-', CreateMethod(
			'-',"(function (b) {return (context.lookup('self').get() - b);})",context));
		context.get('Number').set('*', CreateMethod(
			'*',"(function (b) {return (context.lookup('self').get() * b);})",context));
		

		//compiler.evaluate("Number addMethod:[* b | self / (1/b) ]");
		//compiler.evaluate("Number addMethod:[- b | self + (-1 * b) ]");
		
		context.get('Number').set('>', CreateMethod(
			'>',"(function (b) {if(context.lookup('self').get() > b){return context.lookup('true').get();}else{return context.lookup('false').get();}})",context));
		context.get('Number').set('<', CreateMethod(
			'<',"(function (b) {if(context.lookup('self').get() < b){return context.lookup('true').get();}else{return context.lookup('false').get();}})",context));
		
		compiler.evaluate("Number addMethod:[>= b | (self < b) not ]");
		
		compiler.evaluate("Number addMethod:[<= b | (self > b) not ]");
		compiler.evaluate("Number addMethod:[=b | self Equals:b]");
		context.get('Number').set('toString',CreateMethod('toString',"(function(){ var self = context.lookup('self').get(); return CreateString((self!=null) ? self.toString() : 'Number');})",context));
  	}
	
	function InitializeStrings() {
		compiler.evaluate("String := object basicNew");
		compiler.evaluate("String addMethod:[toString | self]");
		compiler.evaluate("String addMethod:[value | self]");
		
		//concat
		context.get('String').set('+', CreateMethod(
			'+',"(function (b) {return (model.createString(context.lookup('self').get().toString() + b.receive('toString')().text));})",context));  
		//append
		context.get('String').set('append:', CreateMethod(
			'append:',"(function (b) {context.lookup('self').get().append(b.receive('toString')().text);return (context.lookup('self').get());})",context)); 
		//size
		context.get('String').set('size', CreateMethod(
			'size',"(function () {return (context.lookup('self').get().length);})",context)); 
		//toList
		context.get('String').set('toList', CreateMethod(
			'toList',"(function () {return (model.createList(context.lookup('self').get().text.split('').map(e=>CreateString(e))));})",context)); 			
	}
	
	function InitializeLists() {
		compiler.evaluate("List := object basicNew");
	
		context.get('List').set('at:', CreateMethod(
			'at:',"(function (index) {var s=context.lookup('self').get(); if(index>=s.length){return context.lookup('null').get();} return s.get(index); })",context));
		
		context.get('List').set('at:put:', CreateMethod(
			'at:put:',"(function (index,value) { var s=context.lookup('self').get(); while(index>=s.length){s.push(context.lookup('null').get());} context.lookup('self').get().set(index,value); return context.lookup('self').get(); })",context));
			
		context.get('List').set('count', CreateMethod(
			'count',"(function () {return context.lookup('self').get().length; })",context));
		
		context.get('List').set('removeAt:', CreateMethod(
			'removeAt:',"(function (index) {var s =context.lookup('self').get(); s.remove(index); return s; })",context));
		compiler.evaluate('List addMethod:[pop| r:= self at:(self count-1). self removeAt:(self count-1). r].List addMethod:[push: value| self at:(self count) put: value].');				
		
		
		context.get('List').set('do:', CreateMethod(
			'do:',"(function (aBlock) {var s =context.lookup('self').get(); for(var i=0;i<s.length;i++){ aBlock.receive('valueWithArguments:')(model.createList([s.get(i)]));} return s; })",context));
		compiler.evaluate(" List addMethod:[select: aBlock | |temp|  temp := {}.  self do:[:each | temp push:( aBlock valueWithArguments: {each})]]");
		compiler.evaluate("List addMethod: [toString || str index | str := '{'. index := 0. self do: [:each | index = 0 ifFalse: [str append: ' . ']. str append: each toString. index := index + 1]. str append: '}'. str]");
  	}
	
	function InitializeOthers() {		 
		compiler.evaluate("true := ((object basicNew addMethod:[ifTrue:aBlock| aBlock value]) addMethod:[ifFalse:aBlock| ]) addMethod:[ifTrue: aBlock ifFalse: anotherBlock| aBlock value]");
		compiler.evaluate("true addMethod:[toString|'true']. true addMethod:[value|true]");
		compiler.evaluate("false := ((object basicNew addMethod:[ifTrue:aBlock| ]) addMethod:[ifFalse:aBlock| aBlock value]) addMethod:[ifTrue: aBlock ifFalse: anotherBlock| anotherBlock value]");
		compiler.evaluate("false addMethod:[toString|'false']. false addMethod:[value|false]")
		
		//logical operators
		compiler.evaluate("true addMethod:[not| false]. true addMethod:[and: other| other value]. true addMethod:[or: other| true].");
		compiler.evaluate("false addMethod:[not| true]. false addMethod:[and: other| false]. false addMethod:[or: other| other value].");		
	}
		