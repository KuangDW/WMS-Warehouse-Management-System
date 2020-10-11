// $("#input-form").submit(function(e) {

//     var form = $(this);
//     // var url = form.attr('action');
//     console.log(form)
//     // $.ajax({
//     //        type: "POST",
//     //        url: url,
//     //        data: form.serialize(), // serializes the form's elements.
//     //        success: function(data)
//     //        {
//     //            alert(data); // show response from the php script.
//     //        }
//     //      });
//     e.processFormData();
//     function processFormData() {
//         const form = document.forms['input-form'];    // 取得 name 屬性為 form 的表單
//         const name = form.elements.Purchase_date.value;  // 取得 elements 集合中 name 屬性為 name 的值
//         const email = form.elements.RFID_num.value;// 取得 elements 集合中 name 屬性為 email 的值
//         alert("你的姓名是 " + name + "\n電子郵件是 " + email);
//       };
  
//     e.preventDefault(); // avoid to execute the actual submit of the form.
// });


$(document).ready(function() {

    
    // RFID_InputHash_query ();
    $("#RFIDtable").on('click','#InsertintoStockTable',function(){
        console.log('code')

        var code=$(this).parents("tr").find("td").eq(0).text(); //RFID之ID
        console.log(code)
        document.getElementById("RFID_num").value = code;
    });

    $("reloadRFID").click(function(){
        RFID_InputHash_query ();
    });

    $("#start_warehousing").click(function(){
        RFID_input_start();
        setInterval("RFID_InputHash_query ();",2000)
    });

    $("#close_warehousing").click(function(){
        RFID_input_close();
    });

    $("#submit_alldata").click(function (r) {

            var Purchase_date = String(document.getElementById("Purchase_date").value);
            var RFID_num = document.getElementById("RFID_num").value;
            var Supplier = document.getElementById("Supplier").value;
            var Material_num = document.getElementById("Material_num").value;
            var Material_name = document.getElementById("Material_name").value;
            var Quantities = document.getElementById("Quantities").value;
            var Location_ = document.getElementById("Location_").value;
            console.log(Purchase_date);
            var dataDict = {
                "Purchase_date": Purchase_date,
                "RFID_num": RFID_num,
                "Supplier":Supplier,
                "Material_num":Material_num,
                "Material_name":Material_name,
                "Quantities":Quantities,
                "Location_":Location_
            };
            console.log(dataDict);
            // var dataDict2 =  JSON.stringify(Supplier);
            // console.log(dataDict2);
            r.preventDefault();
            $.ajax({
                url: '/inputformintoDB',
                dataType: 'json',
                type: 'GET',
                data: dataDict, 
                success: function (response) {
                    console.log(response.length)
                    if (response.length > 5){
                        
                        document.getElementById("confirm text").innerHTML= 'RFID編號重複，請重新確認'
                    }
                    else {
                      $("#"+RFID_num+"name").parents("tr").hide();
                      document.getElementById("confirm text").innerHTML= '貨品已成功登錄入庫資料'}
                    },
                error: function (e) {
                    console.log("error: ",e);
                }
                }); // end of request the events
            
    });
});


function RFID_Tag_tablefunction(res) {
  
    var RFIDlist_count = (Object.keys(res))
    console.log(res[0])
    for ( i=0 ; i < RFIDlist_count.length ; i++){
      if ($("#"+res[i]+"name").length>0){
        
        // (document.getElementById(res[i]+"name")) == (res[i]))
        console.log('RFID Tag exist')
        
      }
        else{
          console.log($("#"+res[i]+"name").length)
          btsr = '<tr><td style=" vertical-align:middle;" id="'+res[i]+'name">'+ res[i]+'</td><td><button class="btn btn-link" id="InsertintoStockTable" style=" text-align:center;display: flex;"> <i class="fa fa-edit" ></i></td></tr>'
        //   <i class="fa fa-edit" ></i>
          $("#appendRFIDlist").append(btsr);
        }
  }
  }



function RFID_InputHash_query (){
    var RFID_InputHash = $.ajax({
      url: "/RFID_InputHash_query",
      type: "GET",
      dataType: "json",
      success: function (result) {
        console.log(result)
        RFID_Tag_tablefunction(result);
      }
    })
};



function RFID_input_start(){
  var RFID_inputstart = $.ajax({
      url: "/RFID_input_start",
      type: "POST",
      dataType: "json",
      success: function (result) {
        console.log(result)
      }
    })
};


function RFID_input_close(){
  var RFID_inputclose = $.ajax({
      url: "/RFID_input_close",
      type: "POST",
      dataType: "json",
      success: function (result) {
        console.log(result)
      }
    })
};