var now_time = "";
var ago_time = "";
var basenumber = 4;

$(function () {
    now_time = moment().format('YYYY-MM-DD HH:mm:ss');
    ago_time = moment().subtract('24','h').format('YYYY-MM-DD HH:mm:ss');
    
});

// 找POINT格式 必須傳入點格式



// function Historical_data_request() {
// var request_event = $.ajax({
//     url: "/QueryHistoricalData",
//     type: 'GET',
//     dataType: 'json',
//     data: {
//         now_time:now_time,
//         ago_time:ago_time,
//     },
//     success: function (response) {
//         Historical_point[0] = response[0]
//         console.log(Historical_point)
//         console.log(response)
//         },
//     error: function () {
//         console.log("error: ajax request the number failed");
//     }
//     }); // end of request the events
// // update_mychart();
// };

$("#basesetting").click(function (){
    
    var basedata = [];
    for (var i=0 ; i<basenumber ;i++){
        var tmp = [];
        for (var j=0; j<3 ;j++){
            tmp.push($('#base'+i+j).val());
        }
        basedata.push(tmp);
    }
    console.log(basedata[0])
    var transdata2 = {
        "base1" : JSON.stringify(basedata[0].join()),
        "base2" : JSON.stringify(basedata[1].join()),
        "base3" : JSON.stringify(basedata[2].join()),
        "base4" : JSON.stringify(basedata[3].join())
    };

    $.ajax({
        url: "/basesetting",
        type: 'GET',
        dataType: "json",
        data: transdata2,
        success: function (response) {
            console.log(response);

            },
        error: function () {
            console.log("error: ajax request the number failed");
        }
        });
});
