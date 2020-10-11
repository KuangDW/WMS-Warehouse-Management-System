var now_time = "";
var ago_time = "";
var pointsets = new Array;
var Historical_point = new Array;
var datesets=new Array(new Array(1,2),new Array(3,4),new Array(5,6),new Array(7,8));

var coords = [];
var scatterChartData = {
      datasets: [{
          xAxisID: 'x-axis-1',
          yAxisID: 'y-axis-1',
          borderColor: 'black',
          borderWidth: 1,
          // pointBackgroundColor: ['#000', '#00bcd6', '#d300d6'],
          // pointBorderColor: ['#000', '#00bcd6', '#d300d6'],
          pointRadius: 1,
          pointHoverRadius: 1,
          fill: false,
          tension: 0,

          showLine: true,
          // borderColor: window.chartColors,
      //     backgroundColor: setpointcolor[i],
          // showLine: true,
          data: [{}]
  }]
};

globalAlpha = 0.1;

$(function () {
    now_time = moment().format('YYYY-MM-DD HH:mm:ss');
    ago_time = moment().subtract('24','h').format('YYYY-MM-DD HH:mm:ss');
    
    mychart();
    Historical_data_request();
    // update_mychart();
    globalAlpha = 0.1;
    getbackground();
});

// 找POINT格式 必須傳入點格式
function storeCoordinate(xVal, yVal, array) {
  array.push({x: xVal, y: yVal});
}



function Historical_data_request() {
var request_event = $.ajax({
    url: "/QueryHistoricalData",
    type: 'GET',
    dataType: 'json',
    data: {
        now_time:now_time,
        ago_time:ago_time,
    },
    success: function (response) {
        console.log(response)
        // console.log(response[x].length)
        // const xlist = (response[0]['x'])
        
        // const ylist = (response[0]['y'])

        Pathlength = []

        for (i=0 ; i < response['3D5F'][0]['x'].length; i++){
          var cc = { 'x': parseInt(response['3D5F'][0]['x'][i]), 'y': parseInt(response['3D5F'][0]['y'][i]) }
          console.log(cc)
          scatterChartData.datasets[0].data.push(cc)
          if (i>0){
            distancebetween2point = Math.sqrt(Math.pow(response['3D5F'][0]['x'][i]-response['3D5F'][0]['x'][i-1],2)+ Math.pow(response['3D5F'][0]['y'][i]-response['3D5F'][0]['y'][i-1],2) + Math.pow(response['3D5F'][0]['z'][i]-response['3D5F'][0]['z'][i-1],2));
            Pathlength.push(parseInt(distancebetween2point));
          }
        }
        var sumofpath = Pathlength.reduce(function(prev, element) {
          // 與之前的數值加總，回傳後代入下一輪的處理
          return prev + element;
        }, 0);
        // scatterChartData.datasets[0].data = JSON.parse(JSON.stringify(response[0]))
        // scatterChartData.datasets[0].data = [aa];
        console.log(scatterChartData)
        console.log(sumofpath)
        // 網頁表單呈現輸入
        document.getElementById('Point0leng').innerHTML = sumofpath;
        window.myScatter.update();
        // for(i in response[0].length){
          // storeCoordinate(Historical_point[0][i],Historical_point[1][i],coords);
        // }
        
        // for (i in response){
          
        //   Historical_point.push(response[i])
        // }
        
        // console.log(Historical_point)
        },
    error: function () {
        console.log("error: ajax request the number failed");
    }
    }); // end of request the events
// update_mychart();
    

};


function mychart() {
  
  var ctx = document.getElementById('heyChart2').getContext('2d');
  window.myScatter = Chart.Scatter(ctx, {
      data: scatterChartData,
      // showLine: true,
      options: {
        backgroundColor: { fill:'transparent' },
        legend:{ 
          display:false },
          responsive: true,
          hoverMode: 'nearest',
          intersect: true,
          animation: {
                duration: 0
            },
          title: {
              display: true,
              text: 'Chart.js Scatter Chart - Multi Axis'
          },
          scales: {
              xAxes: [{
                  type: 'linear',
                  position: 'bottom',
                  ticks: {min: -500 , max:300},
                  gridLines: false
                      // zeroLineColor: 'rgba(0,0,0,1)'
                      
                  
              }],
              yAxes: [{
                  type: 'linear', // only linear but allow scale type registration. This allows extensions to exist solely for log scale for instance
                  display: true,
                  position: 'left',
                  id: 'y-axis-1',
                  ticks: {min: -500 , max:300},
                  gridLines: false
                    // zeroLineColor: 'rgba(0,0,0,1)'
                    
                
              }],
          },
      
        plugins: {
          zoom: {
            // Container for pan options
            pan: {
              // Boolean to enable panning
              enabled: true,
              mode: 'xy',
              // rangeMin: {
                // Format of min pan range depends on scale type
                // x: 25,
                // y: 25
              // },
              // rangeMax: {
                // Format of max pan range depends on scale type
                // x: 25,
                // y: 25
              // },
              speed: 20,
              threshold: 10,
            },
            zoom: {
              // Boolean to enable zooming
              enabled: true,
            
              // Enable drag-to-zoom behavior
              // drag: true,
              // rangeMin: {
                // Format of min pan range depends on scale type
                // x: 25,
                // y: 25
              // },
              // rangeMax: {
                // Format of max pan range depends on scale type
                // x: 25,
                // y: 25
              // }
              mode: 'xy',
              // Speed of zoom via mouse wheel
              // (percentage of zoom on a wheel event)
              // speed: 0.1,
            
              // Minimal zoom distance required before actually applying zoom
              // threshold: 2,
            
              // On category scale, minimal zoom level before actually applying zoom
              // sensitivity: 3,
            
              // Function called while the user is zooming
            }
          }
        },
      },
  }
  );
  // window.myScatter.update();
};


// function update_mychart(){
//   console.log(Historical_point)

//   var arr = Historical_point[0[1]];
//   console.log(arr)
//     pointsets.push(     
//         [
//           {
//               // label: 'datapoint1'+i,
//               xAxisID: 'x-axis-1',
//               yAxisID: 'y-axis-1',
//               pointBorderWidth:2.5,
//               pointRadius :6,
            
//               // borderColor: window.chartColors,
//             //   backgroundColor: setpointcolor[i],
//               showLine: true,
//               data : {x:11, y:22}
            
//           }])
//           console.log(pointsets)
//     scatterChartData['datasets'] = pointsets;
//     console.log(scatterChartData)
// };




function getbackground(){
  var thisInterval = setInterval(function(){
    //this if statment checks if the id "thisCanvas" is linked to something
    if(document.getElementById('heyChart2') != null){
      //do what you want
      const canvas = document.getElementById('heyChart2');
      const canvas2 = document.getElementById('heyChart');
      console.log(canvas)
      const ctx = canvas.getContext('2d');
      const ctx2 = canvas2.getContext('2d');
      console.log(ctx)
      var img = new Image();
      img.src = "static/vendor/34.jpg";
      img.onload = function () {
          ctx2.canvas.width = ctx.canvas.width;
          ctx2.canvas.height = (ctx.canvas.height)-30;
          ctx2.drawImage(img, 0, 0);
          ctx2.drawImage(img, 0 ,0,canvas.width,canvas.height);
      };

      window.addEventListener('load', resize, false);
      window.addEventListener('resize', resize, false); 





      // backgroundimage = onImageLoaded('static/vendor/34.jpg',drawImage(image)); // Using optional size for image
      // image.onload = drawImageActualSize; // Draw when image has loaded
      // Load an image of intrinsic size 300x227 in CSS pixels
      // ctx2.drawImage(image,36,27,canvas.width,canvas.height);
      // canvas.width = this.naturalWidth;
      // canvas.height = this.naturalHeight;
      // Will draw the image as 300x227, ignoring the custom size of 60x45
      // given in the constructor
      // ctx2.drawImage(this, 0, 0);
      // To use the custom size we'll have to specify the scale parameters 
      // using the element's width and height properties - lets draw one 
      // on top in the corner:
      // ctx2.drawImage(backgroundimage,36,15,canvas.width,canvas.height);
      clearInterval(thisInterval);
      console.log('testing')

      // will remove the interval if you have given your interval a name
    }
  //the 500 means that you will loop through this every 500 milliseconds (1/2 a second)
},500)
};


function onImageLoaded(url, cb) {
  var image = new Image()
  image.src = url

  if (image.complete) {
    // 圖片已經被載入
    cb(image,36,15,canvas.width,canvas.height)
  } else {
    // 如果圖片未被載入，則設定載入時的回調
    image.onload = function () {
      cb(image,36,15,canvas.width,canvas.height)
    }
  }
}



    //  * Scale proportionally: If the width of the canvas > the height, the canvas height
    //  * equals the height of the browser window. Else, the canvas width equals the width of the browser window.
    //  * If the window is resized, the size of the canvas changes dynamically.
    //  */
function resize() {
    var ratio = canvas.width / canvas.height;
    var canvas_height = window.innerHeight;
    var canvas_width = canvas_height * ratio;
    if(canvas_width>window.innerWidth){
        canvas_width=window.innerWidth;
        canvas_height=canvas_width/ratio;
    
    canvas.style.width = canvas_width + 'px';
    canvas.style.height = canvas_height + 'px';
}
}




 // STEP 1: MAKE A MAP AND ADD LAYERS
 var map = L.map('map').setView([45.526, -122.667], 13);

 L.esri.basemapLayer('Topographic').addTo(map);

 // create and add a feature layer
 // features from this layer will appear in the Chart.js scatterplot
 var treesFeatureLayer = L.esri.featureLayer({
   url: 'https://www.portlandmaps.com/arcgis/rest/services/Public/Parks_Misc/MapServer/21/'
 });

 treesFeatureLayer.addTo(map);

 // STEP 2: DEFINE A CHART
 // this is a static scatterplot chart definition for now, but it will
 // soon become dynamic by responding to map and feature layer events
 var initialChartData = {
   datasets: [{
     label: 'Portland Heritage Trees',
     // the data values are empty at this moment
     // and will be updated dynamically below
     data: []
   }]
 };

 var chartOptions = {
   scales: {
     xAxes: [{
       scaleLabel: {
         display: true,
         labelString: 'tree diameter'
       },
       ticks: {
         beginAtZero: true,
         max: 250,
         stepSize: 50
       }
     }],
     yAxes: [{
       scaleLabel: {
         display: true,
         labelString: 'tree height'
       },
       ticks: {
         beginAtZero: true,
         max: 250,
         stepSize: 50
       }
     }]
   },
   maintainAspectRatio: false,
   // turn off animations during chart data updates
   animation: {
     duration: 0
   },
   // see STEP 4 below
   onHover: handleChartHover
 };

 var chart = new Chart('chartCanvas', {
   type: 'scatter',
   data: initialChartData,
   options: chartOptions
 });

 // STEP 3: MAKE THE CHART DYNAMIC BY ESTABLISHING MAP-TO-CHART COMMUNICATION
 // show in the scatterplot only the features in the map's current extent
 // by handling several events from both the map and feature layer
 map.on('zoom move', updateChart);
 treesFeatureLayer.on('load', updateChart);

 function updateChart () {
   // reformat the features' attributes of interest into
   // the data array format required by the Chart.js scatterplot
   var scatterPlotDataArray = [];

   treesFeatureLayer.eachActiveFeature(function (e) {
     // loop over each active feature in the map extent and
     // push an object into the scatterPlotDataArray in this format:

     // {
     //   x: diameter attribute value,
     //   y: height attribute value,
     //   featureId: unique ID for chart-to-map communication in STEP 4
     // }

     scatterPlotDataArray.push({
       x: e.feature.properties.DIAMETER,
       y: e.feature.properties.HEIGHT,
       featureId: e.feature.id
     });
   });

   // assign the new scatterPlotDataArray to the chart's data property
   chart.data.datasets[0].data = scatterPlotDataArray;

   // finally, instruct the chart to re-draw itself with the new data
   chart.update();
 }

 // STEP 4 (OPTIONAL): ESTABLISH CHART-TO-MAP COMMUNICATION
 // up until now the map and feature layer inform the chart what to render,
 // but interactions with the chart can also influence the map contents
 function handleChartHover (e) {
   var chartHoverData = chart.getElementsAtEvent(e);

   if (!chartHoverData.length) {
     // if there were no data elements found when hovering over the chart,
     // reset any previous styling overrides and return
     treesFeatureLayer.eachFeature(function (e) {
       e.setOpacity(1);
       e.setZIndexOffset(0);
     });

     return;
   }

   // otherwise, bring attention to the features on the map
   // that are currently being hovered over in the chart
   var hoverFeatureIds = chartHoverData.map(function (datum) {
     return chart.data.datasets[0].data[datum._index].featureId;
   });

   treesFeatureLayer.eachFeature(function (e, idx) {
     if (
       hoverFeatureIds.indexOf(e.feature.id) > -1
     ) {
       e.setOpacity(1);
       e.setZIndexOffset(10000);
     } else {
       e.setOpacity(0.1);
     }
   });
 }