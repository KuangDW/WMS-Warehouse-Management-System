    // am4core.ready(function() {

    // // Themes begin
    // am4core.useTheme(am4themes_animated);
    // // Themes end

    // var chart = am4core.create("chartdiv", am4charts.XYChart);
    // chart.paddingRight = 20;

    // var data = [];
    // var visits = 10;
    // for (var i = 1; i < 200; i++) {
    //   // visits += Math.round((Math.random() < 0.5 ? 1 : -1) * Math.random() * 10);
    //   visits += 0.001;
    //   data.push({ date: new Date(2018, 0, i), value: visits });
    // }

    // chart.data = data;

    // // Create Xaxes
    // var dateAxis = chart.xAxes.push(new am4charts.DateAxis());
    // dateAxis.renderer.grid.template.location = 0;
    // dateAxis.minZoomCount = 5;

    // // this makes the data to be grouped
    // dateAxis.groupData = true;
    // dateAxis.groupCount = 500;

    // // Create Yaxes
    // var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());

    // // Create series
    // var series = chart.series.push(new am4charts.LineSeries());
    // series.dataFields.dateX = "date";
    // series.dataFields.valueY = "value";
    // series.tooltipText = "{valueY}";
    // series.tooltip.pointerOrientation = "vertical";
    // series.tooltip.background.fillOpacity = 0.5;

    // chart.cursor = new am4charts.XYCursor();
    // chart.cursor.xAxis = dateAxis;

    // var scrollbarX = new am4core.Scrollbar();
    // scrollbarX.marginBottom = 20;
    // chart.scrollbarX = scrollbarX;

    // }); // end am4core.ready()

    // 之前的程式，現在用不到了
    // https://www.amcharts.com/docs/v4/concepts/axes/



  
    
    var visits = 0;
    var chart = am4core.create("chartdiv", am4charts.XYChart);

    am4core.ready(function() {

      console.log("load charts succes ! ")
      // Themes begin
      am4core.useTheme(am4themes_animated);
      // Themes end7

      chart.paddingRight = 0;

      chart.data = irtidata();
    
      for (var i = 1; i < 200; i++) {
        visits += 0.00001;
        irtidata.push({ scales : i, value2 : visits });
        irtidata.push({ scales : i, value : visits });
      }

    
      // chart.data2 = data2;



    // Create Xaxes
    var scalesAxis = chart.xAxes.push(new am4charts.ValueAxis());
    scalesAxis.dataFields.scales = "scales";

    // Create Yaxes
    var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
    

    // Create series
    var series = chart.series.push(new am4charts.LineSeries());
    series.dataFields.valueX = "scales";
    series.dataFields.valueY = "value";
    series.tooltipText = "{valueY}";
    series.tooltip.pointerOrientation = "vertical";
    series.tooltip.background.fillOpacity = 0.5;

    var series2 = chart.series.push(new am4charts.LineSeries());
    series2.dataFields.valueX = "scales";
    series2.dataFields.valueY = "value2";
    series2.tooltipText = "{valueY2}";
    series2.tooltip.pointerOrientation = "vertical";
    series2.tooltip.background.fillOpacity = 0.5;


    chart.cursor = new am4charts.XYCursor();
    // chart.cursor.xAxis = dateAxis;

    var scrollbarX = new am4core.Scrollbar();
    scrollbarX.marginBottom = 20;
    chart.scrollbarX = scrollbarX;


  });

   // end am4core.ready()





  var update_mychart = function (res) {
    console.log("connect update_mychart succes ! ")

    // 準備數據    
    console.log(res)
    data.push(res)
    

    // 超出範圍，刪除數據
    if (data.length >= 200){
      data.shift();
    }
    
    
    
    // 填入數據
    chart.data = data;
    

  };


// 建立socket連接，等待server“傳送”數據，呼叫callback函数更新圖表
$(document).ready(function() {

  console.log("connect socket ready ! ")
  namespace = '/test';
  var socket = io.connect(location.protocol + '//' + document.domain + ':' + location.port + namespace);


  console.log("connect socket succes ! ")
  socket.on('server_response', function(res) {
    update_mychart(res);
  });

});





////////////////////////////////////////////////////////////////////////////////////////////

// 之前的程式，現在用不到了

// chart.data = [{
//   "scales": 1,
//   "value": 5
// }, {
//   "scales": 2,
//   "value": 4
// }, {
//   "scales": 3,
//   "value": 6
// }];


// // Create Xaxes
// var scalesAxis = chart.xAxes.push(new am4charts.scalesAxis());
// scalesAxis.dataFields.scales = "scales";

// // Create Yaxes
// // var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
// // valueAxis.renderer.grid.template.strokeOpacity = 1;
// // valueAxis.renderer.grid.template.stroke = am4core.color("#A0CA92");
// // valueAxis.renderer.grid.template.strokeWidth = 2;

// // Create series
// var series = chart.series.push(new am4charts.ColumnSeries());
// series.dataFields.valueY = "value";
// series.dataFields.scalesX = "scales";
// series.name = "Sales";

// // Add data


