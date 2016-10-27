
	// Your Client ID can be retrieved from your project in the Google
	// Developer Console, https://console.developers.google.com
	var CLIENT_ID = '763227956228-btgo7elk6eclu3b3fho6sutsi52884io.apps.googleusercontent.com';

	var SCOPES = ['https://www.googleapis.com/auth/drive.metadata'];
	//HiveProject folder.
	var baseFolder= '0B7Fr-eaQNfHDOU81U2VJRU5VbGc'
	var folder = baseFolder;
	
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
		gapi.client.load('drive', 'v2', function () {
			// Get query param "id"
			var param = location.search.substring(1).split("&")
				.map(function (param) { return param.split("="); })
				.find(function (param) { return param[0] === "id"; });
			if (param === undefined) { param = [] };
			openFolder(param[1] || folder);
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
					"</tr>" +
				"</thead>");
			resp.items.forEach(function (file) {
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
									// Let the browser handle it...
									return true;
								}
							}));
				row.append($("<td>")
						.append("<span>" + file.ownerNames.join(", ") + "</span>"));
				row.append($("<td>")
						.append("<span>" + file.quotaBytesUsed + "</span>"));
				row.append($("<td>")
						.append("<span>" + file.modifiedDate + "</span>"));
				
				table.append(row)
			});
		 });
	}
	
	
	function openFolder(id) {
		var state = {
			folder: id
		};
		if (history.pushState !== undefined) {
			history.pushState(state, "", location.pathname + "?id=" + id);
		}
		updateState(state);
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