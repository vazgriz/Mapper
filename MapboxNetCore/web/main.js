var map = null;
var mapCanvas = null;
var grid = null;
var ping = null;
var addGrid = null;
var setGridCenter = null;

(async () =>
{
	await CefSharp.BindObjectAsync("relay");


	ping = function(data) {

		//parentMap.notify(JSON.stringify(data));
		relay.notify(JSON.stringify(data)).then(function (res)
		{
				
		});
	}

	mapboxgl.accessToken = {{accessToken}};
	map = new mapboxgl.Map({
		container: 'map', // container id
		style: {{style}}, // stylesheet location
		center: [0,0], // starting position [lng, lat]
		zoom: 19, // starting zoom
		maxBounds: null,
	});

	map.on('load', function() {
		mapCanvas = map.getCanvasContainer();
		ping({
			"type": "load",
		});
	});

	map.on('style.load', function () {
		ping({
			"type": "ready",
			"path": window.location.href,
		});
	});
		
	var currentCenter = null;
	var currentZoom = null;
	var currentPitch = null;
	var currentBearing = null;

	map.on("render", function() {
		var newCenter = map.getCenter();

		if(currentCenter === null || currentCenter.lat != newCenter.lat || currentCenter.lng != newCenter.lng) {
			currentCenter = newCenter;
			ping({
				"type": "move",
				"center": newCenter,
			});
		}
			
		var newZoom = map.getZoom();

		if(currentZoom === null || currentZoom != newZoom) {
			currentZoom = newZoom;
			ping({
				"type": "zoom",
				"zoom": newZoom,
			});
		}
			
		var newPitch = map.getPitch();

		if(currentPitch === null || currentPitch != newPitch) {
			currentPitch = newPitch;
			ping({
				"type": "pitch",
				"pitch": newPitch,
			});
		}
			
		var newBearing = map.getBearing();

		if(currentBearing === null || currentBearing != newBearing) {
			currentBearing = newBearing;
			ping({
				"type": "bearing",
				"bearing": newBearing,
			});
		}
	});

	function getExtent(lng, lat, size) {
		let dist = Math.sqrt(2 * Math.pow(size / 2, 2));
		let point = turf.point([lng, lat]);
		let topleft = turf.destination(point, dist, -45, { units: 'kilometers' }).geometry.coordinates;
		let bottomright = turf.destination(point, dist, 135, { units: 'kilometers' }).geometry.coordinates;
		return { 'topleft': topleft, 'bottomright': bottomright };
	}

	function getGrid(lng, lat, size) {
		let extent = getExtent(lng, lat, size);
		return turf.squareGrid([extent.topleft[0], extent.topleft[1], extent.bottomright[0], extent.bottomright[1]], 10, { units: 'kilometers' });
	}

	addGrid = function (gridInfo) {
		if (map === null) return;
		grid = gridInfo;

		map.addSource("grid", {
			'type': 'geojson',
			'data': getGrid(gridInfo.lat, gridInfo.lng, gridInfo.size)
		});

		map.addLayer({
			'id': 'gridlines',
			'type': 'fill',
			'source': 'grid',
			'paint': {
				'fill-color': 'white',
				'fill-outline-color': 'black',
				'fill-opacity': 0.25
			}
		});

		ping({
			"type": "gridReady",
		});
	}

	setGridCenter = function (gridInfo) {
		map.getSource('grid').setData(getGrid(gridInfo.lng, gridInfo.lat, gridInfo.size));
	}

	function serializeMouseEvent(e) {
		return {
			"buttons": e.originalEvent.buttons,
			"point": e.point,
			"lngLat": e.lngLat,
			"features": e.features
		}
	}

	function onGridMove(e) {
		grid.lng = e.lngLat.lng;
		grid.lat = e.lngLat.lat;
		setGridCenter(grid);
		ping({
			"type": "mouseMove",
			"data": serializeMouseEvent(e)
		});

		ping({
			"type": "gridMoved",
			"data": { "lng" : grid.lng, "lat": grid.lat }
		});
	}

	function onGridUp(e) {
		grid.lng = e.lngLat.lng;
		grid.lat = e.lngLat.lat;
		setGridCenter(grid);
	
		// Unbind mouse/touch events
		map.off('mousemove', onGridMove);

		ping({
			"type": "gridMoved",
			"data": { "lng": grid.lng, "lat": grid.lat }
		});
	}

	map.on("mousedown", "gridlines", function (e) {
		//Prevent the default map drag behavior.
		e.preventDefault();

		mapCanvas.style.cursor = 'grab';

		map.on('mousemove', onGridMove);
		map.once('mouseup', onGridUp);

		ping({
			"type": "mouseDown",
			"data": serializeMouseEvent(e)
		});
	});

	map.on("mousedown", function(e) {
		ping({
			"type": "mouseDown",
			"data": serializeMouseEvent(e)
		});
	});

	map.on("mousemove", function (e) {
		ping({
			"type": "mouseMove",
			"data": serializeMouseEvent(e)
		});
	});

	map.on("mouseup", function (e) {
		ping({
			"type": "mouseUp",
			"data": serializeMouseEvent(e)
		});
	});

	map.on("mouseenter", function () {
		ping({
			"type": "mouseEnter",
		});
	});

	map.on("mouseleave", function () {
		ping({
			"type": "mouseLeave",
		});
	});

	map.on("click", function () {
		ping({
			"type": "click",
		});
	});

	map.on("dblclick", function () {
		ping({
			"type": "doubleClick",
		});
	});

})();

function exec(expression) {
	var result = eval(expression);

	try {
		return JSON.stringify(result);
	} catch(e) {
		return "null";
	}
}

function run(expression) {
	var f = new Function("(function() { " + expression + " })()");
	f();
}

function addImage(id, base64) {
	var img = new Image();
	img.onload = function () {
		map.addImage(id, img);
	}
	img.onerror = function (errorMsg, url, lineNumber, column, errorObj) {
		ping({
			"type": "error",
			"info": errorMsg,
		});
	}
			
	img.src = "data:image/png;base64," + base64;
}