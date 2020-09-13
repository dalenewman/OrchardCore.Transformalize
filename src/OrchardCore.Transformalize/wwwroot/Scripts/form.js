$(document).ready(function () {

   bsCustomFileInput.init();

   window.Parsley.on('field:error', function () {
      this.$element.closest('div.form-group').addClass("is-invalid").removeClass("is-valid");
      this.$element.addClass("is-invalid").removeClass("is-valid");
   });
   window.Parsley.on('field:success', function () {
      this.$element.closest('div.form-group').removeClass("is-invalid").addClass("is-valid").find('.help-block').empty();
      this.$element.removeClass("is-invalid").addClass("is-valid");
   });
   window.Parsley.addValidator('string', {
      requirementType: 'date',
      validateString: function (value, requirement) {
         return moment(value).isValid();
      },
      messages: {
         en: 'This value should be a date'
      }
   });

   $(document).bind("ajaxSend", function () {
      block();
   }).bind("ajaxComplete", function () {
      $("#busy").hide();
      $.unblockUI();
   });

   $('#id_validate_button').bind('click', function () {
      post();
   });

   function bind(html) {

      $("#id_content").html(html);

      var actionUrl = "";
      if (settings.controller === "Form") {
         actionUrl = settings.updateUrl;
      } else {
         // this is a task or bulk action
         actionUrl = $('#id_submit').val() === "Run" ? settings.runUrl : settings.updateUrl;
      }

      $("#id_form").attr("action", actionUrl);

      $("#id_form").parsley({
         errorsContainer: function (e) {
            return e.$element.closest('div.form-group').find('span.help-container');
         },
         errorsWrapper: '<span class="help-container"></span>',
         errorTemplate: '<span class="help-block"></span> ',
         excluded: 'input[type=button], input[type=submit], input[type=reset], input[type=file]',
         inputs: 'input, textarea, select, input[type=hidden]'
      });

      if (settings.geo) {
         getLocation();
      }
      setFocus();

      // if it is post back, then validate field and post back
      $("select[data-tfl-post-back='true'],input[data-tfl-post-back='true'],textarea[data-tfl-post-back='true']").change(function () {
         if ($(this).parsley().isValid()) {
            post();
         } else {
            $(this).parsley().validate();
         }
      });

      // if it's not post back, just validate the field
      $("select[data-tfl-post-back='false'],input[data-tfl-post-back='false'],textarea[data-tfl-post-back='false']").change(function () {
         $(this).parsley().validate();
      });

      // block automatic form submit on enter
      $("select,input:not(input[type='submit'])").keydown(function (e) {
         var code = e.keyCode || e.which;
         if (code === 13) {
            e.preventDefault();
            // move to next field
            var inputs = $(this).closest('form').find(':input');
            inputs.eq(inputs.index(this) + 1).focus();
            return false;
         }
         return true;
      });

      // track focus for back-end
      // file input focus needs help since jquery file upload hides it
      $("select,input,textarea").focusin(function () {
         $("#id_focus").val($(this).attr("name"));
         if ($(this).attr('type') === 'file') {
            $(this).parent().addClass('focus');
         }
         console.log($(this).attr("name") + " has focus");
      });

      // file input needs help since jquery file upload hides it
      $("input").focusout(function () {
         if ($(this).attr('type') === 'file') {
            $(this).parent().removeClass('focus');
         }
      });

      $("#id_form").submit(function (e) {
         block();
      });

   }

   function post() {
      $.ajax({
         url: settings.ajaxUrl,
         type: "POST",
         data: $("#id_form").serialize(),
         success: function (html) {
            bind(html);
         },
         error: function (html) {
            bind(html);
         }
      }, "html");
   }

   function setFocus() {

      var name = $('#id_focus').val();
      console.log('setting focus to ' + name);
      var $target = $('#id_' + name);

      $target.focus().select();

      // ios doesn't refresh dropdowns when ajax re-populates
      if (navigator.userAgent.match(/(ip(hone|od|ad))/i)) {
         if ($target.is("select") && $target.closest("div").prev().has("select").length > 0) {
            $target.blur();
         }
      }
   }

   function getLocation() {
      if ("geolocation" in navigator) {

         if ($('#id_geo_latitude').val() === "") {
            navigator.geolocation.getCurrentPosition(
               function (location) {
                  console.log(location.coords);
                  $('#id_geo_latitude').val(location.coords.latitude);
                  $('#id_geo_longitude').val(location.coords.longitude);
                  $('#id_geo_accuracy').val(location.coords.accuracy);
               },
               function (error) {
                  console.log(error);
               }
            );
         }

         navigator.geolocation.getCurrentPosition(
            function (location) {
               console.log(location.coords);
               $('#id_geo_latitude').val(location.coords.latitude);
               $('#id_geo_longitude').val(location.coords.longitude);
               $('#id_geo_accuracy').val(location.coords.accuracy);
            },
            function (error) {
               console.log(error);
            },
            {
               enableHighAccuracy: true,
               maximumAge: 15000,
               timeout: Infinity
            }
         );

      } else {
         console.log("geolocation IS NOT available");
      }
   }

   function block() {
      $('#busy').show();
      $.blockUI({
         message: null,
         css: {
            border: 'none',
            padding: '15px',
            backgroundColor: '#000',
            '-webkit-border-radius': '10px',
            '-moz-border-radius': '10px',
            opacity: .5,
            color: '#fff',
            baseZ: 1021
         }
      });
   }

   bind();
});