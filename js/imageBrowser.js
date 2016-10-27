
	// Your Client ID can be retrieved from your project in the Google
	// Developer Console, https://console.developers.google.com
	var CLIENT_ID = '763227956228-btgo7elk6eclu3b3fho6sutsi52884io.apps.googleusercontent.com';

	var SCOPES = ['https://www.googleapis.com/auth/drive.metadata'];
	//HiveProject folder.
	var folder = '0B7Fr-eaQNfHDOU81U2VJRU5VbGc';
	
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
			openFolder(param[1] || "root");
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
						"<th>Stars</th>" +
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
				appendStars(file, row);
				
				table.append(row)
			});
		 });
	}
	
	function appendStars(file, row) {
		var td = $("<td>");
		var prop = undefined;
		if (file.properties !== undefined) {
			prop = file.properties.find(function (p) { return p.key === "stars"; });
		}
		var value = prop !== undefined ? prop.value : 0;
		var stars = [];
		for (var i = 0; i < 5; i++) {
			(function (index) {
				var star = $("<span>");
				stars.push(star);
				star.attr("class", "glyphicon glyphicon-star")
					.css("color", index >= value ? "grey" : "goldenrod")
					.on("click", function () {
						stars.forEach(function (e, i) {
							e.css("color", i > index ? "grey" : "goldenrod");
						});
						value = index + 1;
						var request = gapi.client.drive.properties.insert({
							'fileId': file.id,
							'resource': {
								'key': "stars",
								'value': value,
								'visibility': 'PRIVATE'
							}
						});
						request.execute(function(resp) { console.log(resp); });
						
						// Let our server know
						ajax.request({
							type: 'POST',
							url: '/events/classify',
							data: {
								document_id: file.id,
								stars: value
							},
							success: function (resp) {								
								console.log(resp);
							},
							error: function (err) {
								console.log(err);
							}
						});
					})
					.on("mouseover", function () {
						stars.forEach(function (e, i) {
							e.css("color", i > index ? "grey" : "goldenrod");
						});
					})
					.on("mouseout", function () {
						stars.forEach(function (e, i) {
							e.css("color", i >= value ? "grey" : "goldenrod");
						});
					});
				td.append(star);
			})(i);
		}
		row.append(td);
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
		folder = state.folder || "root";
		listFiles();
	}
	
	window.onpopstate = function (e) {
		if (e.state !== null) {
			updateState(e.state);
		}
	}