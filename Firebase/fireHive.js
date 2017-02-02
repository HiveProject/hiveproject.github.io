let hive = (function () {
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
		for (let[key, value]of this) {
			if (value == inputValue)
				return key
		}
		return undefined;
	}
	
	let module = {
		config: {
			apiKey: " AIzaSyA-Y_mz58xgvGkQNK_tQCXQiG3q1mlA6hM",
			authDomain: "hive-1336.firebaseapp.com",
			databaseURL: "https://hive-1336.firebaseio.com/",
			storageBucket: "hive-1336.appspot.com"
		}
	};
	let database = null;
	//this map contains key -> obj
	let loadedObjects = new Map();
	//this map contains root name -> key
	let roots = new Map();
	
	//this map contains obj -> proxy
	let proxies = new Map();
	//this map contains proxy -> handler
	let handlers = new Map();
	
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
	module.removeElement = function (proxy) {
		//the obj i get here should be a proxy.
		let id = loadedObjects.getKey(proxies.getKey(proxy));
		if (id) {
			let key = roots.getKey(id);
			if(key){
				module.remove(key);
			} 
		}
	}
	module.set =function(key,obj){
		let id = loadedObjects.getKey(obj);
		if (!id) {
			id=innerAdd(obj);
		} 
		database.ref("roots/" + key).set(id);
		return getProxy(obj); 
	};
	module.get=function(key)
	{
		return getProxy(loadedObjects.get(roots.get(key)));
	};
	module.keys=function()
	{
		return Array.from(roots.keys());
	};
	module.elements=function()
	{
		let result=new Map();
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
		let id = loadedObjects.getKey(obj);
		if (id) {
			return id;
		} 
		let key = database.ref("objects").push().key;
		loadedObjects.set(key, obj);
		updateFields(obj,Object.keys(obj));
		return key;
	};


	//this is to hold the unloaded childs.
	let missingReferences=[];
	
	function checkForRefrences(key, obj) {
		//to use.
		let toExecute = missingReferences.filter(function (item) {
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
			let obj = {};
			let received = dataSnapshot.val();
			loadedObjects.set(dataSnapshot.key, obj);
			mapSnapshotToObject(obj,received);
			checkForRefrences(dataSnapshot.key, obj);
		}
	}

	function childRemoved(oldDataSnapshot) { 
		let obj= loadedObjects.get(oldDataSnapshot.key);
		unsuscribeProxy(proxies.get(obj));
		handlers.delete(proxies.get(obj));
		proxies.delete(obj); 
		loadedObjects.delete (oldDataSnapshot.key);
	}
	function childChanged(dataSnapshot) {
		let obj = loadedObjects.get(dataSnapshot.key);
		let received = dataSnapshot.val();
		mapSnapshotToObject(obj,received);
	}
	
	function mapSnapshotToObject(obj,received)
	{
		for (let k in received) {
			if (received[k] != null) {
				//todo: this fails if the value is something that would trigger this condition
				
				if (received[k].type=="null"){
					//i have a null here
					obj[k]=null;
				}else if(received[k].type == "Object") {
					//if the object is not in my cache, i might have some sync issues here.
					let other = loadedObjects.get(received[k].value);
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
	function updateFields(obj,fieldNames){
		let upd = {};
		let id = loadedObjects.getKey(obj);
		if (id) {
			fieldNames.forEach(function(fieldName) 
			{
				let basePath = "/" + id + "/" + fieldName + "/";
				//first of all i need to see if the value is either null or undefined.
				let value=obj[fieldName];
				if(value==null || value==undefined)
				{
					obj[fieldName]=null;//this is just to ensure that no undefined is left here.
					upd[basePath+"type"]="null";
					upd[basePath+"value"]=null;
				}else{
					let type = value.constructor.name;
					upd[basePath + "type"] = type;
					if (type == "Object") {
						upd[basePath + "value"] = innerAdd(value);
					} else if (type=="Date"){
						upd[basePath+"value"]=value.toJSON();
					}else {
						upd[basePath + "value"] = value;
					}
				}
		
			} );
			database.ref("objects").update(upd);
		}
	}
	function updateField(obj, fieldName) {
		updateFields(obj,[fieldName]);
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
			let proxy=proxies.get(obj);
			let handler=handlers.get(proxy);
			//ensure the handler is enabled.
			if(!handler.get)
			{
				handler.get=getExecuted;
				handler.set=setExecuted;
			}
			return proxy;
		} 
		//create handler
		let handler = {get:getExecuted,set:setExecuted};
		let proxy= new Proxy(obj,handler);
		proxies.set(obj,proxy);
		handlers.set(proxy,handler);
		return proxy;
	}
	function unsuscribeProxy(proxy)
	{
		if(handlers.has(proxy))
		{
			let handler = handlers.get(proxy);
			handler.get=undefined;
			handler.set=undefined;
		}
	}
	
	function getExecuted(target,property,rcvr)
	{
		let result =target[property];
		if(result.constructor.name=="Object")
			return getProxy(result);
		return result;
	}
	function setExecuted(target,property,value,rcvr)
	{ 
		//if i have something null/undefined, then valueOf will fail
		if(target[property]== null ||target[property]== undefined 
			||value== null||value==undefined){
				//if they are different.
				if(target[property]!=value|| !(property in target))
				{
					target[property]=value;
					updateField(target, property);
				}
			}
		else if (target[property].valueOf() != value.valueOf()) {
				target[property]=value;
				updateField(target, property);
			}
		return true;
	}
	
	
	
	
	//GC
	let initializedGC=false;
	function doGC(){
		if(!initializedGC)
		{
			initializedGC=true;		
			let touchedElements=[];
			setTimeout(function(){ 
				roots.forEach(function(value,key){mark(loadedObjects.get(value),touchedElements);});
				sweep(touchedElements);	 
				initializedGC=false;
			}, 2000);	
			
		}
	};
	function mark(obj,arr)
	{
		let type = obj.constructor.name;
		if (type == "Object") {
			let id = loadedObjects.getKey(obj);
			if (id) {
				if(!arr.some(function(k){return k===id;})){
					arr.push(id);
					for (let k in obj) {
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
	{	let upd = {};
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
