
    // 建立socket連接，等待server“傳送”數據，呼叫callback函数更新圖表
      // 填入數據
    // 連接後執行update_mychart()
    $(document).ready(function() {
      

      console.log("connect socket ready ! ")
      namespace = '/test';
      var socket = io.connect(location.protocol + '//' + document.domain + ':' + location.port + namespace);


      console.log("connect socket succes ! ")
      socket.on('server_response', function (res) {
        console.log(res)
        if (Object.keys(res).length == 0 ){
          getbackground();  
          return false;
        } else {
        update_mychart(res);
        TableOnefunction_data_request(res);
        console.log(res)
        getbackground();
      }
      });
      mychart();
    });
    var RestoJsOb = [];
    var CardID = {};
    var obj = new Object; 
    var original_point = new Array(new Array());
    var timesteamperr = [[]];
    var x = new Array(new Array());
    var y = new Array(new Array());
    var z = new Array(new Array());
    var j = 0;
    var timeseries = [];
    var countstep = 0;
    var distancelist = new Array();
    var Pathlength=new Array(new Array());
    var TotalPath=new Array(new Array());
    var Xaxisdata=new Array(new Array(),new Array());
    var Yaxisdata=new Array(new Array(),new Array());
    var Zaxisdata=new Array(new Array(),new Array());
    var datesets=new Array(new Array(),new Array());
    var Pathlength_dict = {};
    var TotalPath_dict = {};
    var Xaxisdata_dict = {};
    var Yaxisdata_dict = {};
    var Zaxisdata_dict = {};
    var datesets_dict = {};
    var setpointcolor = ['#90cd8a','#FF0000', '#87CEFA','#FFD306'];
    var OpacityColor = ['#00000033']
    var scatterChartData = { datasets :{} };
    var original_time = new Array(new Array());
    console.log(original_point.length)
    var pointstyle = 'circle';
    var update_mychart = function (res) {

      console.log("connect update_mychart succes ! ")
      console.log(res)
      console.log(Object.keys(res).length)
      
      console.log(Object.keys(res)[0])//3D5F
      var resCardID = (Object.keys(res))
      console.log(resCardID)
      console.log(window.setpointcolor[0])
      for ( i=0 ; i < resCardID.length ; i++){


          if (Object.keys(window.CardID).includes(resCardID[i])){
            console.log(window.CardID,'1')
          }else{
            window.CardID[resCardID[i]] = window.setpointcolor.pop();
            console.log(window.CardID,'2')
            timesteamperr[resCardID[i]] = '';
            Pathlength_dict[resCardID[i]] = new Array();
            TotalPath_dict[resCardID[i]] = new Array();
            Xaxisdata_dict[resCardID[i]] = new Array();
            Yaxisdata_dict[resCardID[i]] = new Array();
            Zaxisdata_dict[resCardID[i]] = new Array();
            datesets_dict[resCardID[i]] = new Array();
      }
      }


      // 準備數據
      console.log(typeof(res))
      // original_point = res[resCardID[1]]['1'] ;
      // console.log(original_point)
      for (i = 0; i< resCardID.length ; i++){
        // 卡片狀態確認(更換樣式) 
        
        if (res[resCardID[i]]['0'] > 0){
          pointstyle = 'triangle';
        } else {
          pointstyle = 'circle';
        }

        // 資料點紀錄
          Xaxisdata = Xaxisdata_dict[resCardID[i]];
          Yaxisdata = Yaxisdata_dict[resCardID[i]];
          Zaxisdata = Zaxisdata_dict[resCardID[i]];
          // datesets = datesets_dict[resCardID[i]];


          Xaxisdata.unshift(parseInt(res[resCardID[i]]['1'][0]));
          Yaxisdata.unshift(parseInt(res[resCardID[i]]['1'][1]));
          Zaxisdata.unshift(parseInt(res[resCardID[i]]['1'][2]));
          // datesets.unshift(new Date(res[resCardID[i]]['2']));



          console.log(res[resCardID[i]]['1'][0])
        
        if (countstep >1 ){
            x =  (res[resCardID[i]]['1'][0]) - (Xaxisdata[1]);
            y =  (res[resCardID[i]]['1'][1]) - (Yaxisdata[1]);
            z =  (res[resCardID[i]]['1'][2]) - (Zaxisdata[1]);
            // distancebetween2point = Math.sqrt( Math.pow(x,2)+ Math.pow(y,2) + Math.pow(z,2));
            // distancelist = Pathlength_dict[resCardID[i]];
            // distancelist.unshift(parseInt(distancebetween2point));
            
          } else if (countstep >= 15) {
            Xaxisdata.pop() ;
            Yaxisdata.pop() ;
            Zaxisdata.pop() ;
            // datesets.pop() ;
            // distancelist.pop();
          }
          // if ( < 1){
          
          //   original_point = { [resCardID[i]] : Object.values(res[resCardID[i]]['1'])} ;
          //   // original_time[resCardID[i]] = new Date(res[resCardID[i]]['2']);
          //   j += 1
          //   console.log(res[resCardID[i]]['1'])
          //   console.log(original_point)
          //   console.log(original_point[resCardID[i]][0])
          // } 
          
          // if( timesteamperr )

          Xaxisdata_dict[resCardID[i]] = Xaxisdata;
          Yaxisdata_dict[resCardID[i]] = Yaxisdata;
          Zaxisdata_dict[resCardID[i]] = Zaxisdata;
          // datesets_dict[resCardID[i]] = datesets;
          // Pathlength_dict[resCardID[i]] = distancelist;


            // var jqole = timesteamperr[resCardID[i]];
            // console.log
            // timeseries[i].push((new Date(res[resCardID[i]]['2']) - new Date(original_time[resCardID[i]]))/1000);
            // timesteamperr[resCardID[i]] = timeseries[i];



            // TotalPath[resCardID[i]][0].unshift(parseInt(distancebetween2point));
            // console.log(Pathlength.length)
            
            // console.log(countstep)
   

            
            // timeseries[i] = ((new Date(res[resCardID[i]]['2']) - new Date(original_time[resCardID[i]]))/1000);
            
            // timesteamperr[resCardID[i]].unshift(timeseries[i]);
            // distancebetween2point = Math.sqrt( Math.pow(x[i],2)+ Math.pow(y[i],2) + Math.pow(z[i],2));
            // Pathlength[resCardID[i]].unshift(parseInt(distancebetween2point));



            // TotalPath[resCardID[i]].unshift(parseInt(distancebetween2point));
            // timesteamperr[[resCardID[i]][0].pop();
            // Pathlength[[resCardID[i]][0].pop();
            // original_point[resCardID[i]] = res[resCardID[i]]['1'] ;
            // original_time[resCardID[i]] = new Date(res[resCardID[i]]['2']);
          
          
        
            // console.log(timesteamperr)
            // console.log(Pathlength)

        
          // if (countstep <11){
          //   // RestoJsOb.push(res)  ;

          // }
          //   else if (countstep >= 11){
          //       // RestoJsOb.push(res)  ;
          //       // RestoJsOb.shift() ;

          //   }

        //   console.log(TotalPath)
        //   var color = Chart.helpers.color;
          
          // 資料點色碼
          // var setpointcolor = ['#90cd8a','#FF0000', '#87CEFA','#FFD306'];
          // 當54行之for迴圈的i=0，象徵第一張卡片，create new array pointsets
          if (i == 0){ 
            var pointsets = new Array;
          }
          // pointsets 存取同卡片最近五點資訊
          // var cardcolor = window.CardID[resCardID[i]];

          pointsets.push(     
            [
              {
                  label: [resCardID[i]],
                  xAxisID: 'x-axis-1',
                  yAxisID: 'y-axis-1',
                  pointBorderWidth:2.5,
                  pointRadius :12,
                  pointStyle: pointstyle,
                  // borderColor: window.chartColors,
                  backgroundColor: window.CardID[resCardID[i]],
                  data: [{
                      x: Xaxisdata[0],
                      y: Yaxisdata[0]
                  }]
              }
              , {
                // label: 'datapoint2' +i,
                xAxisID: 'x-axis-1',
                yAxisID: 'y-axis-1',
                pointBorderWidth:2.0,
                pointRadius :10,
                pointStyle: pointstyle,
                // borderColor: window.chartColors,
                backgroundColor: window.CardID[resCardID[i]],
                data: [{
                    x: Xaxisdata[1] ,
                    y: Yaxisdata[1],
                }]
              },{
                // label: 'datapoint3' +i,
                xAxisID: 'x-axis-1',
                yAxisID: 'y-axis-1',
                pointBorderWidth:1.5,
                pointRadius :8,
                pointStyle: pointstyle,
                // borderColor: window.chartColors,
                backgroundColor: window.CardID[resCardID[i]],
                data: [{
                    x: Xaxisdata[2] ,
                    y: Yaxisdata[2],
                }]
              },{
                // label: 'datapoint4' +i,
                xAxisID: 'x-axis-1',
                yAxisID: 'y-axis-1',
                pointBorderWidth:1,
                pointRadius :6,
                pointStyle: pointstyle,
                // borderColor: window.chartColors,
                backgroundColor: window.CardID[resCardID[i]],
                data: [{
                    x: Xaxisdata[3] ,
                    y: Yaxisdata[3],
                }]
              },  {
                // label: 'datapoint5' +i,
                xAxisID: 'x-axis-1',
                yAxisID: 'y-axis-1',
                pointBorderWidth:0.75,
                pointRadius :4,
                pointStyle: pointstyle,
                // borderColor: window.chartColors,
                backgroundColor: window.CardID[resCardID[i]],
                data: [{
                    x: Xaxisdata[4] ,
                    y: Yaxisdata[4],
                }]
              }
            ]
            );
            

        }  // 54行迴圈結束
        countstep += 1 ;





        // console.log(pointsets.data)
        // 將二維陣列pointsets轉存成一維陣列
        var newArr = [];
        for(var i = 0; i < pointsets.length; i++){
        newArr = newArr.concat(pointsets[i]);
        }
        
        //將點位資訊傳入scatterChartData.datasets
        // console.log(typeof(newArr))
        window.scatterChartData['datasets'] = newArr ;
        console.log(scatterChartData)
        // for(var i = 0; i < pointsets.length; i++){
          
        //   var sumofpath = Pathlength[i].reduce(function(prev, element) {
        //     // 與之前的數值加總，回傳後代入下一輪的處理
        //     return prev + element;
        //   }, 0);
        // //
        //   var Totalsumofpath = TotalPath[i].reduce(function(prev, element) {
        //     // 與之前的數值加總，回傳後代入下一輪的處理
        //     return prev + element;
        //   }, 0);

        //   var sumoftime = timesteamperr[i].reduce(function(prev, element) {
        //     // 與之前的數值加總，回傳後代入下一輪的處理
        //     return prev + element;
        //   }, 0);
        //   // console.log(sumofpath)
        //   // console.log(sumoftime)
        //   if  (isNaN(sumofpath)){
        //     sumofpath = 0;
        //   }else if(isNaN(sumoftime)) {
        //     sumoftime = 0;
        //   }else if(isNaN(Totalsumofpath)){
        //     Totalsumofpath = 0;
        //   }

        //   var speedrate = (sumofpath/sumoftime).toFixed(2);  
        //   if (isNaN(speedrate)){
        //     speedrate = 0;
        //   } else if (isFinite(speedrate) != true ) {
        //     speedrate = 0;
        //   }
        //     document.getElementById("Point"+i+"x").innerHTML = Xaxisdata[i][0];
        //     document.getElementById("Point"+i+"y").innerHTML = Yaxisdata[i][0];
        //     document.getElementById("Point"+i+"z").innerHTML = Zaxisdata[i][0];
        //     document.getElementById("Point"+i+"leng").innerHTML = Totalsumofpath;
        //     document.getElementById("Point"+i+"spee").innerHTML = speedrate;
          
        //   }
          window.myScatter.update();
        };
            
            
        


      
      
function mychart() {
  var ctx = document.getElementById('heyChart').getContext('2d');
  window.myScatter = Chart.Scatter(ctx, {
      data: window.scatterChartData,
      showLine: true,
      options: {
        aspectRatio: 0.25,
        maintainAspectRatio:false,
        legend:{ 
          display:false 
          },
          responsive: true,
          hoverMode: 'nearest',
          intersect: true,
          animation: {
                duration: 0
            },
          title: {
              display: false,
              text: 'Chart.js Scatter Chart - Multi Axis'
          },
          scales: {
              xAxes: [{
                  type: 'linear',
                  position: 'bottom',
                  ticks: {min: -613 , max:0},
                  gridLines: {
                    display:false
                      // zeroLineColor: 'rgba(0,0,0,1)'
                  }
              }],
              yAxes: [{
                  type: 'linear', // only linear but allow scale type registration. This allows extensions to exist solely for log scale for instance
                  display: true,
                  position: 'left',
                  id: 'y-axis-1',
                  gridLines: {
                    display:false
                      // zeroLineColor: 'rgba(0,0,0,1)'
                  },
                  ticks: {min: 0 , max:626},
              }],
          },
      
          // plugins: {
            // zoom: {
              // Container for pan options
              // pan: {
                // Boolean to enable panning
                // enabled: true,
                // mode: 'xy',
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
                // speed: 20,
                // threshold: 10,
              // },
              // zoom: {
                // Boolean to enable zooming
                // enabled: true,
              
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
                // mode: 'xy',
                // Speed of zoom via mouse wheel
                // (percentage of zoom on a wheel event)
                // speed: 0.1,
              
                // Minimal zoom distance required before actually applying zoom
                // threshold: 2,
              
                // On category scale, minimal zoom level before actually applying zoom
                // sensitivity: 3,
              
                // Function called while the user is zooming
              // }
            // }
          },
      },
  // }
  );
  // window.myScatter.update();
};
    //   };


function change() // no ';' here
{
    var elem = document.getElementById("startbutton");
    if (elem.value=="關閉定位"){
      shutdowncommand ();
      elem.value = "開啟定位";
    } 
    else {
      startcommand ();
      elem.value = "關閉定位";
    } 
}
    

function startcommand (){
  var startmsg = $.ajax({
    url: "/startmission",
    type: "POST",
    dataType: "text",
    success: function (result) {
      console.log(result)
    }
  })
};


function shutdowncommand (){
  var shutdownmsg = $.ajax({
    url: "/endmission",
    type: "POST",
    dataType: "text",
    success: function (result) {
      console.log(result)
    }
  })
};

function getsettingtable (){
  var basetable=document.getElementById('setbase');
  console.log(basetable)
  console.log(basetable.rows[1].cells[1].children[0].children[0].value )
}


function TableOnefunction_data_request(res) {
  
  var resCardID = (Object.keys(res))
  
  for ( i=0 ; i < resCardID.length ; i++){
    if ( $("#"+resCardID[i]+"name").length > 0){
      // document.getElementById(resCardID[i]+"name").value == resCardID[i])
      
      document.getElementById(resCardID[i]+"x").innerHTML = parseInt(res[resCardID[i]]['1'][0]);
      document.getElementById(resCardID[i]+"y").innerHTML = parseInt(res[resCardID[i]]['1'][1]);
      document.getElementById(resCardID[i]+"z").innerHTML = parseInt(res[resCardID[i]]['1'][2]);
    }
      else{
        btsr = '<tr><td><div id ="'+ resCardID[i]+'color" style= " width: 30px; height: 30px;border-radius: 15px; background-color :' + CardID[resCardID[i]]+ '"></div></td><td id="'+resCardID[i]+ 'name">'+ resCardID[i] +'</td><td id = "status'+resCardID[i]+'" style = "text-align:center;"></td>  <td id="'+resCardID[i]+'x">'+ parseInt(res[resCardID[i]]['1'][0]) +'</td><td id="'+resCardID[i]+'y">'+ parseInt(res[resCardID[i]]['1'][1]) +'</td><td id="'+resCardID[i]+'z">'+ parseInt(res[resCardID[i]]['1'][2]) +'</td><td>'+"TEST" +'</td><td>'+ "TEST" +'</td></tr>'
    
        $("#appendtablepoint").append(btsr);
      }

    if (parseInt(res[resCardID[i]]['1'][0]) > -372 && parseInt(res[resCardID[i]]['1'][1]) > 384 ){
        document.getElementById("status"+resCardID[i]).innerHTML='<span  class="fa fa-exclamation" style="color: #FF0000; font-size:18px"></span>';
      }else{
        document.getElementById("status"+resCardID[i]).innerHTML='';
      }
}
}


function getbackground(){
  var thisInterval = setInterval(function(){
    //this if statment checks if the id "thisCanvas" is linked to something
    if(document.getElementById('heyChart') != null){
      //do what you want
      const canvas = document.getElementById('heyChart');
      const canvas2 = document.getElementById('heyChart2');
      console.log(canvas)
      const ctx = canvas.getContext('2d');
      const ctx2 = canvas2.getContext('2d');
      console.log(ctx)
      var img = new Image();
      img.src = "static/vendor/36.jpg";
      img.onload = function () {
          ctx2.canvas.width = ctx.canvas.width;
          ctx2.canvas.height = (ctx.canvas.height)-10;
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
// function (){
//     var GetBaseStation = $.ajax({
//         url: "api/v1.0/uncheck_msg",
//         type: "GET",
//         dataType: "json",
//         success: function(response) {
//             var count = 0;
//             for(i in response){
//                var id = response[i]['m_id'].replace(/\./g,"");
               
//                $('#nexttype' + id).attr('style',  'background-color:#FFED97');
//                $('#grid_' + id).attr('style',  'background-color:#FFED97');
//             //    style="background-color:yellow"
//                 count++;
//             }
//             if(count > 0)
//             {
// 		console.log('there are some unseen msgs');
//                 $("#secondNotification")[0].play();
//             }
//             else{
//                 console.log("no unseen message.");
//             }
//         },
//         error: function(){
//             console.log("did not get the unseen msg");
//         }
//     })
//     setTimeout('getUnseenMsg()', 10000);
// }