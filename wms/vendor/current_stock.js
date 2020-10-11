var Hash_RFID_LIST = new Array();

$(document).ready(function() {
    
    $("#Select_form").on('change', '#Section_select', function(){
        var categories = document.getElementById("Section_select").value;
        console.log(categories)
        $.ajax({
           url:"/inventory_query",				
           method:"GET",
           data:{
              categories:categories
           },					
           success:function(res){					
              console.log(res)
              Setting_inventory_table(res);
           },
           error: function (e) {
            console.log("error: ",e);
            }

        });//end ajax
    });

    $("#inventory_Manual").click(function(){
        var Check_RFIDlist = new Array();
        var Section = document.getElementById("Section_select").value;
        checkbox_get_RFIDlist(Check_RFIDlist);
        console.log(Check_RFIDlist)
        Check_RFIDlist_string = JSON.stringify(Check_RFIDlist)
    
        $.ajax({
            url:"/update_inventoryInfo",				
            method:"GET",
            data:{
                Section:Section,
                Check_RFIDlist: Check_RFIDlist_string
            },					
            success:function(res){					
               console.log(res)
               Setting_inventory_table(res);
               $('input').iCheck('uncheck');

            },
            error: function (e) {
             console.log("error: ",e);
             }
    
         });

    });

    $("#inventory_auto").click(function(){
        var elem = document.getElementById("inventory_auto");
        if (elem.innerText=="結束自動盤點"){
            RFID_input_close();
            RFID_InputHash_query(RFID_update_inventoryInfo);
        //   console.log(RFID_InputHash_query(Check_RFIDlist))
        //   console.log(typeof(Check_RFIDlist))
            // console.log(window.Hash_RFID_LIST)
            // input_Hash_RFID_LIST = window.Hash_RFID_LIST;
            // RFID_update_inventoryInfo(RFID_InputHash_query);
            elem.innerText = "開啟自動盤點";
        } 
        else {
            RFID_input_start();
            elem.innerText = "結束自動盤點";
    
        } 
    
    });

});


function Setting_inventory_table(res){
    $("#inventory_append_table").html('');
    var now_time = new Date();
    var colorsetting= 'table-primary';
    for ( i=0 ; i < res.length ; i++){
        if((i&1)===0){
            var trclass="even pointer";
            }else{
            var trclass="odd pointer";
            }
        
        var DateofDBitem = new Date(res[i][8]);
  
        // var timeNum = 8;
        DateofDBitem.setHours((DateofDBitem.getHours()-8));
        console.log(DateofDBitem)
        var diffoftime = parseInt(parseInt(now_time - DateofDBitem) / 1000 / 60);
        console.log(diffoftime);//兩個時間相差的分鐘數

        if (diffoftime < 5){
            colorsetting = 'table-primary';
        }else{
            colorsetting = '';
        }
        var btsr = '<tr class="'+trclass+'" >\
        <td class="a-center ">\
        <input type="checkbox" class="flat" name="table_records" value="'+res[i][0]+'">\
        </td>\
        <td class=" ">'+res[i][2]+'</td>\
        <td class=" ">'+res[i][3]+'</td>\
        <td class=" ">'+res[i][6] +'</td>\
        <td class=" ">'+res[i][0] +'</td>\
        <td class=" ">'+res[i][1] +'</td>\
        <td class=" ">'+res[i][4] +'</td>\
        <td class=" ">'+res[i][5] +' 區</td>\
        <td class="'+colorsetting+'" >'+res[i][8] +'</td>\
      </tr>'
        //   <i class="fa fa-edit" ></i>
        $("#inventory_append_table").append(btsr);
    }
};






function checkbox_get_RFIDlist (Check_RFIDlist){
    $('input:checkbox:checked[name="table_records"]').each(function(i) { Check_RFIDlist[i] = this.value; });
};



function RFID_input_start(){
    var RFID_inputstart = $.ajax({
        url: "/testing_rfid",
        type: "POST",
        dataType: "json",
        success: function (result) {
          console.log(result)
        }
      })
  };


function RFID_input_close(){
    var RFID_inputclose = $.ajax({
        url: "/testing_rfid",
        type: "POST",
        dataType: "json",
        success: function (result) {
          console.log(result)
        }
      })
  };




function RFID_InputHash_query (callback){
    var RFID_InputHash = $.ajax({
      url: "/RFID_InputHash_query",
      type: "GET",
      dataType: "json",
      success: function (result) {
        window.Hash_RFID_LIST = result;
        console.log(result)
        callback();
      }
    })
};


function RFID_update_inventoryInfo (){
    var Section = document.getElementById("Section_select").value;
    
    var Check_RFIDlist_string = JSON.stringify(window.Hash_RFID_LIST);
    $.ajax({
        url:"/update_inventoryInfo",				
        method:"GET",
        data:{
            Section:Section,
            Check_RFIDlist: Check_RFIDlist_string
        },					
        success:function(res){					
           console.log(res)
           Setting_inventory_table(res);
        },
        error: function (e) {
         console.log("error: ",e);
         }

     });
};


    
