
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
	/**
	 * Load Drive API client library.
	 */
	function loadDriveApi() {
		//load realtime api.
		
		gapi.load('auth:client,drive-realtime,drive-share', function(){
			gapi.client.load('drive', 'v2', function () {
				// Get query param "id"
				var param = location.search.substring(1).split("&")
					.map(function (param) { return param.split("="); })
					.find(function (param) { return param[0] === "id"; });
				if (param === undefined) { param = [] };
				document.getElementById('add').onclick =createFile;
				openFolder(param[1] || folder);
			});
		});
		
	}
	/**
	 * Print files.
	 */
	function listFiles() {
		var request = gapi.client.drive.files.list({
			orderBy: ['folder', 'modifiedDate'],
			spaces: ['drive'],
			corpus: 'DOMAIN',
			maxResults: 30,
			q: "'" + folder + "' in parents and trashed = false"
		});
		request.execute(function(resp) {
			var table = $(output);
			table.html("" + 
				"<thead>" +
					"<tr>" +
						"<th>Name</th>" +
						"<th>Owner</th>" +
						"<th>File size</th>" +
						"<th>Last modified</th>" +
						"<th>Actions</th>"+
					"</tr>" +
				"</thead>");
			resp.items.forEach(function (file) {
				if(file.mimeType.startsWith(hiveMimeType)){
					var row = $("<tr>");
					row.append($("<td>")
							.append($("<a>")
								.attr("href", file.alternateLink)
								.append($("<img>").attr("src", file.iconLink))
								.append($("<span>").text(file.title)))
								.on("click", function () {
									if (file.mimeType === "application/vnd.google-apps.folder") {
										// Open the folder inside the app
										openFolder(file.id);
										return false;
									} else {
										if(file.mimeType.startsWith(hiveMimeType)){
											//i need to open the image. The real mimeType is something like "application/vnd.google-apps.drive-sdk.763227956228". Those numbers appear as soon as realtime initializes the file.
											openImage(file.id);
											return false;
										} else {
											// Let the browser handle it...
											return true;
										}
									}
								}));
					row.append($("<td>")
							.append("<span>" + file.ownerNames.join(", ") + "</span>"));
					var sizeNode = $("<td>");
					getBytes(file,function(size){sizeNode.append("<span>" + size + "</span>")});
					row.append(sizeNode);						
					row.append($("<td>")
							.append("<span>" + file.modifiedDate + "</span>"));
					row.append(appendActionButtons($("<td>"),file.id));
					table.append(row);
				}
					
			});
			hideLoading();
		 });
	}
	/*
	*Creates the buttons to handle the actions on a single file
	*/
	function appendActionButtons(node,fileId){
		node.append('<a class="btn btn-info" id="rename" onclick="renameFile(\''+fileId+'\')"><i class="fa fa-pencil fa-lg"></i></a>');
		node.append('<a class="btn btn-warning" id="collect" onclick="collectFile(\''+fileId+'\')"><i class="fa fa-recycle fa-lg"></i></a>');
		node.append('<a class="btn btn-primary" id="clone" onclick="cloneFile(\''+fileId+'\')"><i class="fa fa-files-o fa-lg"></i></a>');
		node.append('<a class="btn btn-danger" id="delete" onclick="deleteFile(\''+fileId+'\')"><i class="fa fa-trash fa-lg"></i></a>');
		
		return node;
	}
	/*
	*Creates a new file on the current folder.
	*/
	function createFile(){
		showLoading();
		var metadata = {
			'title': 'New Hive Image',
			'mimeType': hiveMimeType,
			'parents':[{"id":baseFolder}] 
			};
		gapi.client.drive.files.insert(metadata).execute(updateState);
	}
	function deleteFile(fileId)
	{
		showLoading();
		var request = gapi.client.drive.files.delete({'fileId': fileId});
		request.execute(updateState);
	}
	function cloneFile(fileId)
	{
		showLoading();
		gapi.client.drive.files.get({'fileId':fileId}).execute(function (res){
			var metadata = {
				'title': res.title,
				'mimeType': hiveMimeType,
				'parents':[{"id":baseFolder}] 
			};
			gapi.client.drive.files.insert(metadata).execute(function (cres){
				gapi.drive.realtime.load(fileId, function(doc) {
					var originalModel=doc.getModel();
					var newDoc = gapi.drive.realtime.loadFromJson(originalModel.toJson());
					console.log("Compressed from " + originalModel.bytesUsed + " To "+ newDoc.getModel().bytesUsed);
					doc.close();
					newDoc.saveAs(cres.id);					
					//the saveAs actually takes some time, and if while it happens i refresh the page then i have some issues because asking for weight 
					//initializes the file and starts giving that annoying concurrent exception.
					setTimeout(function(){newDoc.close();updateState([]);} , 8000); 
				} );
			}); 
		});
	}	
	function collectFile(fileId)
	{
		showLoading();
		gapi.client.drive.files.get({'fileId':fileId}).execute(function (res){
			var metadata = {
				'title': res.title,
				'mimeType': hiveMimeType,
				'parents':[{"id":baseFolder}] 
			};
			gapi.client.drive.files.insert(metadata).execute(function (cres){
				gapi.drive.realtime.load(fileId, function(doc) {
					var originalModel=doc.getModel();
					var newDoc = gapi.drive.realtime.loadFromJson(originalModel.toJson());
					console.log("Compressed from " + originalModel.bytesUsed + " To "+ newDoc.getModel().bytesUsed);
					doc.close();
					newDoc.saveAs(cres.id);
					//the saveAs actually takes some time, and if while it happens i refresh the page then i have some issues because asking for weight 
					//initializes the file and starts giving that annoying concurrent exception. 
					setTimeout(function(){newDoc.close();deleteFile(fileId);} , 8000);  
				} );
			}); 
		});
	}
	function renameFile(fileId)
	{
		gapi.client.drive.files.get({'fileId':fileId}).execute(function (res){
			$("#renameBox").val(res.title);
			$("#renameId").val(fileId);
			$("#renameBox").focus();	
			$("#renameBox").select();	
			$('#renameModal').modal('toggle');	
		});
	}
	function doRename(modal)
	{
		$('#renameModal').modal('toggle');	
		showLoading();
		
		var body = {'title': $("#renameBox").val()};
		gapi.client.drive.files.patch({
			'fileId': $("#renameId").val(),
			'resource': body
			}).execute(updateState); 		
	}
	/*
	*Gets the total realtime bytes used for the given file
	*/
	function getBytes(file,callback){
		gapi.drive.realtime.load(file.id,function(doc){
			var size = doc.getModel().bytesUsed
			doc.close();
			callback(size);
		});
	}
	function openFolder(id) {
		showLoading();
		var state = {
			folder: id
		};
		if (history.pushState !== undefined) {
			history.pushState(state, "", location.pathname + "?id=" + id);
		}
		updateState(state);
	}
	function openImage(id){
		window.location.href = '/image.html?id='+id;
	}
	function updateState(state) {		
		folder = state.folder || folder;
		listFiles();
	}
	
	window.onpopstate = function (e) {
		if (e.state !== null) {
			updateState(e.state);
		}
	}
	var loading=false;
	function showLoading(){
		if(!loading){
			loading=true;
			$('#loadingModal').modal('toggle');
		}
	}
	function hideLoading(){
		if(loading){
			loading=false;
			$('#loadingModal').modal('toggle');
		}	
	}