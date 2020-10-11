$(document).ready(function() {


    // 製作表單，並製作揀貨單號


    





    // <div id = "wizardclass" class = "stepContainer" style="height: 269px;"></div>\


    // var element= document.getElementsByClassName('buttonFinish btn btn-default');
    // for(var i=0;i<element.length;i++){
    //   element[i].onclick = function(){
    //    console.log('click');
    //   }
    // }
    $("#pickupmission").click(function(){
        var Check_RFIDlist = ['b808099f48881c293e00f1a','c3e8e9eeefcccfd6c5a10fba','b808099f48880e893801416','d4c1c7dfc9c4e1ee93e00fb9'];
        // checkbox_get_RFIDlist(Check_RFIDlist);
        // console.log(Check_RFIDlist)
        Check_RFIDlist_string = JSON.stringify(Check_RFIDlist)
    
        $.ajax({
            url:"/pickup_query",				
            method:"GET",
            data:{
                Check_RFIDlist: Check_RFIDlist_string
            },					
            success:function(res){					
               console.log(res)
               PickupRFIDnumlist(res);

            },
            error: function (e) {
             console.log("error: ",e);
             }
    
         });

    });






});
// <small>Step 1 description</small>

        //   <li>
        //     <a href="#step-2">
        //       <span class="step_no">2</span>
        //       <span class="step_descr">
        //                         Step 2<br />
        //                         <small>Step 2 description</small>
        //                     </span>
        //     </a>
        //   </li>
        //   <li>
        //     <a href="#step-3">
        //       <span class="step_no">3</span>
        //       <span class="step_descr">
        //                         Step 3<br />
        //                         <small>Step 3 description</small>
        //                     </span>
        //     </a>
        //   </li>
        //   <li>
        //     <a href="#step-4">
        //       <span class="step_no">4</span>
        //       <span class="step_descr">
        //                         Step 4<br />
        //                         <small>Step 4 description</small>
        //                     </span>
        //     </a>
        //   </li>
        
function PickupRFIDnumlist(res){
    var picknumber = '69115908';
    
    var pickupstring = '\
    <div class="row">\
    <div class="col-md-12 col-sm-12 ">\
    <div class="x_panel" id = "append_table" >\
    <div class="x_title">\
    <h2>揀貨單'+picknumber+'</h2>\
    <div class="clearfix"></div>\
    </div>\
    <div class="col-md-8 col-sm-12 ">\
    <div class="x_content">\
      <div id="wizard" class="form_wizard wizard_horizontal">\
        <ul id = "wizard_steps" class="wizard_steps">\
        </ul>\
        </div>\
        </div>\
        </div>' ;
    $("#anchor_point").append(pickupstring);

    var j = 0;
    for ( i=0 ; i < 3  ; i++){
        var pickupstep = '\
              <li>\
                <a href="#step-'+(i+1)+'">\
                  <span class="step_no" >'+(i+1)+'</span>\
                  <span class="step_descr">\
                                    Step'+(i+1)+'<br />\
                                    </span>\
                </a>\
              </li>';

        $("#wizard_steps").append(pickupstep);
    

        if((j&1)===0){
        var trclass="even pointer";
        }else{
        var trclass="odd pointer";
        }

        var pickupcontent = '\
            <div id="step-'+(i+1)+'">\
            <p>\
            <table class="table table-striped jambo_table bulk_action">\
            <thead>\
              <tr class="headings" id= "headingssetting">\
                <th>\
                </th>\
                <th class="column-title" id="column-setting">品項編碼</th>\
                <th class="column-title" id="column-setting">品項名稱 </th>\
                <th class="column-title" id="column-setting">進貨日期 </th>\
                <th class="column-title" id="column-setting">RFID編號</th>\
                <th class="column-title" id="column-setting">供應商</th>\
                <th class="column-title" id="column-setting">庫存數量</th>\
                <th class="column-title" id="column-setting">擺放區域</th>\
                <th class="bulk-actions" id="column-setting1" colspan="8">\
                <a class="antoo" style="color:#fff; font-weight:500;">已選取 ( <span class="action-cnt"> </span> ) <i class="fa fa-chevron-down"></i></a>\
              </th>\
              </tr>\
            </thead>\
            <tbody id = "inventory_append_tablestep'+(i+1)+'">\
            </tbody>\
            </table>\
            </p>\
            </div>';

        $("#wizard").append(pickupcontent);
            var pickupitems= '<tr class="'+trclass+'" >\
            <td class="a-center ">\
            <input type="checkbox" id ="'+res[j][0]+'check" class="flat" name="table_records" value="'+res[j][0]+'">\
            </td>\
            <td class=" ">'+res[j][2]+'</td>\
            <td class=" ">'+res[j][3]+'</td>\
            <td class=" ">'+res[j][6] +'</td>\
            <td id="'+res[j][0]+'class" class=" ">'+res[j][0] +'</td>\
            <td class=" ">'+res[j][1] +'</td>\
            <td class=" ">'+res[j][4] +'</td>\
            <td class=" ">'+res[j][5] +' 區</td>\
            </tr>';

            $("#inventory_append_tablestep"+(i+1)).append(pickupitems);
        if (i == 2){
            j+=1

            var pickupitems= '<tr class="'+trclass+'" >\
            <td class="a-center ">\
            <input type="checkbox" id ="'+res[j][0]+'check" class="flat" name="table_records" value="'+res[j][0]+'">\
            </td>\
            <td class=" ">'+res[j][2]+'</td>\
            <td class=" ">'+res[j][3]+'</td>\
            <td class=" ">'+res[j][6] +'</td>\
            <td id="'+res[j][0]+'class" class=" ">'+res[j][0] +'</td>\
            <td class=" ">'+res[j][1] +'</td>\
            <td class=" ">'+res[j][4] +'</td>\
            <td class=" ">'+res[j][5] +' 區</td>\
            </tr>';


            $("#inventory_append_tablestep"+(i+1)).append(pickupitems);
        }

        j +=1


        }

        
        var RFIDlistAppend = '\
        <div class="col-md-4 col-sm-12 ">\
        <div class="x_content">\
        <table class="table table-striped" id = "RFIDtable" >\
        <a class="badge badge-primary" style="font-size:18px; margin: 0 auto;display: block; color:white">即時盤點</a>\
            <thead>\
                <tr>\
                    <!-- <th width = "15%">Label</th> -->\
                    <th width = "100%">Tag_ID</th>\
                </tr>\
            </thead>\
            <tbody id= "appendRFIDlist" >\
            </tbody>\
            </table>\
            </div>\
            </div>';
        $("#append_table").append(RFIDlistAppend);
        
        setInterval("RFID_InputHash_query ();",2000)





        $('#wizard').smartWizard({
            onFinish: function() {
                if($('input:checkbox:checked[name="table_records"]').length< res.length){
                    alert('揀貨尚未完成')
                }else{
                    console.log("Finished");
                    alert('揀貨完成')
                }
                  //選擇所有的被checked的表單元素
                
                 //does work
            }
          });
    };
        


    // <ul class="nav navbar-right panel_toolbox">\
    // <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>\
    // </li>\
    // </ul>\


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
          btsr = '<tr><td style=" vertical-align:middle;" id="'+res[i]+'name">'+ res[i]+'</td></tr>'
        //   <i class="fa fa-edit" ></i>
          $("#appendRFIDlist").append(btsr);
        }

        console.log($('#'+res[i]+'class').length)
      if ($('#'+res[i]+'class').length > 0 ){
            $('#'+res[i]+'check').prop("checked", true);
            document.getElementById(res[i]+'name').setAttribute('Class', 'table-primary');
            // setAttribute('id', '100);

      }
  };
  };



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
    