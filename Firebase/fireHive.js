var hive = (function () {
	if (!firebase) {
		//firebase should be included.
		debugger;
	}
	if (!Proxy) {
		//i need proxy to work .
		debugger;
	}
	if(!Map)
	{
		//i need Map to work.
		debugger;
	}
	//set new method to Map
	Map.prototype.getKey = function (inputValue) {
		for (var[key, value]of this) {
			if (value == inputValue)
				return key
		}
		return undefined;
	}
	
	var module = {
		config: {
			apiKey: " AIzaSyA-Y_mz58xgvGkQNK_tQCXQiG3q1mlA6hM",
			authDomain: "hive-1336.firebaseapp.com",
			databaseURL: "https://hive-1336.firebaseio.com/",
			storageBucket: "hive-1336.appspot.com"
		}
	};
	var database = null;
	var loadedObjects = new Map();
	var roots = new Map();
	
	//this map contains obj -> proxy
	var proxies = new Map();
	//this map contains proxy -> handler
	var handlers = new Map();
	
	module.start = function () {
		roots.clear();
		loadedObjects.clear();
		firebase.initializeApp(module.config);
		database = firebase.database();
		handlers.forEach( function(value,key){
			unsuscribeProxy(key);			
		});
		proxies.clear();
		handlers.clear();
		
		
		database.ref("roots").on("child_added", rootAdded);
		database.ref("roots").on("child_changed", rootAdded);
		database.ref("roots").on("child_removed", rootRemoved); 
		
		database.ref("objects").on("child_added", childAdded);
		database.ref("objects").on("child_removed", childRemoved);
		database.ref("objects").on("child_changed", childChanged);
		return module;
	}

	module.remove = function (key) {
		database.ref("roots/" + key).set(null);
		doGC();
	}
	module.removeElement = function (obj) {
		var id = loadedObjects.getKey(obj);
		if (id) {
			var key = roots.getKey(id);
			if(key){
				module.remove(key);
			} 
		}
	}
	module.add =function(key,obj){
		var id = loadedObjects.getKey(obj);
		if (!id) {
			id=innerAdd(obj);
		} 
		database.ref("roots/" + key).set(id);
		return getProxy(obj); 
	};
	module.at=function(key)
	{
		return getProxy(loadedObjects.get(roots.get(key)));
	};
	module.keys=function()
	{
		return Array.from(roots.keys());
	};
	module.elements=function()
	{
		var result=new Map();
		 Array.from(roots.keys()).forEach(function(key){
			result.set(key,getProxy( loadedObjects.get(roots.get(key)))); 
		}); 
		return result;
	};
	
	module.forEach=function(callback)
	{
		Array.from(roots.keys()).forEach(function(key){
			callback(key,getProxy(loadedObjects.get(roots.get(key))));
		}); 
		
		return module;
	};
	//internal stuff
	function innerAdd (obj) {
		var id = loadedObjects.getKey(obj);
		if (id) {
			return id;
		} 
		var key = database.ref("objects").push().key;
		loadedObjects.set(key, obj);
		var data = {};
		for (var k in obj) {
			if (obj[k] != null) {
				data[k] = {
					type: obj[k].constructor.name
				};
				if (data[k].type == "Object") {

					data[k].value = innerAdd(obj[k]);
				} else if (data[k] != "Function") {
					data[k].value = obj[k];
				} else {
					//to do.
					debugger;

				}
			}
		} 
 
			database.ref("objects/" + key).set(data); 
		return key;
	};


	//this is to hold the unloaded childs.
	var missingReferences=[];
	
	function checkForRefrences(key, obj) {
		//to use.
		var toExecute = missingReferences.filter(function (item) {
				return item.key == key;
			});
		missingReferences = missingReferences.filter(function (item) {
				return item.key != key;
			});
		toExecute.forEach(function (item) {
			item.action(obj);
		});
	}
	
	function childAdded(dataSnapshot) {
		if (!loadedObjects.has(dataSnapshot.key)) {
			var obj = {};
			var received = dataSnapshot.val();
			loadedObjects.set(dataSnapshot.key, obj);
			mapSnapshotToObject(obj,received);
			checkForRefrences(dataSnapshot.key, obj);
		}
	}

	function childRemoved(oldDataSnapshot) { 
		var obj= loadedObjects.get(oldDataSnapshot.key);
		unsuscribeProxy(proxies.get(obj));
		handlers.delete(proxies.get(obj));
		proxies.delete(obj); 
		loadedObjects.delete (oldDataSnapshot.key);
	}
	function childChanged(dataSnapshot) {
		var obj = loadedObjects.get(dataSnapshot.key);
		var received = dataSnapshot.val();
		mapSnapshotToObject(obj,received);
	}
	
	function mapSnapshotToObject(obj,received)
	{
		for (var k in received) {
			if (received[k] != null) {
				//todo: this fails if the value is something that would trigger this condition
				if(!received[k].value)
				{debugger;}
				if (received[k].type == "Object") {
					//if the object is not in my cache, i might have some sync issues here.
					var other = loadedObjects.get(received[k].value);
					if (!other) {
						missingReferences.push({
							key: received[k].value,
							action: function (child) {
								obj[k] = child;
							}
						});
					}
					if (obj[k] != other) {
						obj[k] = other;
					}
				} else if(received[k].type == "Date"){
					obj[k]=new Date(received[k].value);
					
				}else{
					//let's say this is a literal for now.
					if (obj[k] != received[k].value) { 
						obj[k] = received[k].value; 
					}
				}
			}
		}
	}
	
	function updateField(obj, fieldName) {
		var upd = {};
		var id = loadedObjects.getKey(obj);
		if (id) {
			var basePath = "/" + id + "/" + fieldName + "/";
			var type = obj[fieldName].constructor.name;

			upd[basePath + "type"] = type;
			if (type == "Object") {
				upd[basePath + "value"] = innerAdd(obj[fieldName]);
			} else if (type=="Date"){
				upd[basePath+"value"]=obj[fieldName].toJSON();
			}else {
				upd[basePath + "value"] = obj[fieldName];
			}
			database.ref("objects").update(upd);
		}
	}
	
	function rootAdded(dataSnapshot){
		 roots.set(dataSnapshot.key,dataSnapshot.val());	 
	}
	function rootRemoved(oldDataSnapshot) {
		roots.delete(oldDataSnapshot.key);
	} 
	
	function getProxy(obj)
	{
		if(proxies.has(obj))
		{
			var proxy=proxies.get(obj);
			var handler=handlers.get(proxy);
			//ensure the handler is enabled.
			if(!handler.get)
			{
				handler.get=getExecuted;
				handler.set=setExecuted;
			}
			return proxy;
		} 
		//create handler
		var handler = {get:getExecuted,set:setExecuted};
		var proxy= new Proxy(obj,handler);
		proxies.set(obj,proxy);
		handlers.set(proxy,handler);
		return proxy;
	}
	function unsuscribeProxy(proxy)
	{
		if(handlers.has(proxy))
		{
			var handler = handlers.get(proxy);
			handler.get=undefined;
			handler.set=undefined;
		}
	}
	
	function getExecuted(target,property,rcvr)
	{
		var result =target[property];
		if(result.constructor.name=="Object")
			return getProxy(result);
		return result;
	}
	function setExecuted(target,property,value,rcvr)
	{
		//todo: if value is null or undefined this fails.
		if (target[property].valueOf() != value.valueOf()) {
				target[property]=value;
				updateField(target, property);
			}
		return true;
	}
	
	
	
	
	//GC
	var initializedGC=false;
	function doGC(){
		if(!initializedGC)
		{
			initializedGC=true;		
			var touchedElements=[];
			setTimeout(function(){ 
				roots.forEach(function(value,key){mark(loadedObjects.get(value),touchedElements);});
				sweep(touchedElements);	 
				initializedGC=false;
			}, 2000);	
			
		}
	};
	function mark(obj,arr)
	{
		var type = obj.constructor.name;
		if (type == "Object") {
			var id = loadedObjects.getKey(obj);
			if (id) {
				if(!arr.some(function(k){return k===id;})){
					arr.push(id);
					for (var k in obj) {
						if (obj[k]) {
							mark(obj[k],arr);
						}
					}
				}
			} 
		} else if(type=="Array") {
			debugger; //todo
		}else{
			//do nothing.
		} 
	};
	function sweep(aliveObjects)
	{	var upd = {};
		//todo: optimize.
		Array.from(loadedObjects.keys()).forEach(function(key){
			if(aliveObjects.indexOf(key)==-1){
				upd["/"+key]=null; 
			}
		});
		database.ref("objects").update(upd);
	};
	//GC 
	
	
	return module;
}
	().start());
