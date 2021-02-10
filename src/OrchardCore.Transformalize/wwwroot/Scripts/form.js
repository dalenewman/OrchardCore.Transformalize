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

      if (settings.location.enabled) {
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

      $("#id_location_button").click(function () {
         getLocation();
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

      var button = $('#id_location_button');
      var span = $('#id_location_accuracy');

      if ("geolocation" in navigator) {

         var locationOptions = {
            enableHighAccuracy: settings.location.enableHighAccuracy,
            maximumAge: settings.location.maximumAge < 0 ? Infinity : settings.location.maximumAge,
            timeout: settings.location.timeout < 0 ? Infinity : settings.location.timeout
         }

         var locationSuccess = function (location) {
            console.log(location);
            settings.location.latitude.val(location.coords.latitude);
            settings.location.longitude.val(location.coords.longitude);
            settings.location.accuracy.val(location.coords.accuracy);
            settings.location.altitude.val(location.coords.altitude);
            settings.location.altitudeaccuracy.val(location.coords.altitudeAccuracy);
            settings.location.speed.val(location.coords.speed);
            settings.location.heading.val(location.coords.heading);

            button.toggleClass("btn-danger btn-success");
            span.text(location.coords.accuracy);
            button.find('div.spinner-border').hide();
            setTimeout(function () {
               span.text("");
               button.find('span.fa-location-arrow').fadeIn();
            }, 3000);
         }

         var locationError = function (error) {
            console.log(error.message);
            switch (error.code) {
               case 1:
                  span.text("Not Allowed")
                  break;
               case 2:
                  span.text("Unavailable")
                  break;
               default:
                  span.text("Timeout")
                  break;
            }
            button.toggleClass("btn-success btn-danger")
            button.find('span.fa-location-arrow').show();
            button.find('div.spinner-border').hide();
            setTimeout(function () {
               span.text("");
               button.find('span.fa-location-arrow').fadeIn();
               button.toggleClass("btn-danger btn-success")
            }, 3000);
         }

         button.find('span.fa-location-arrow').hide();
         button.find('div.spinner-border').show();
         navigator.geolocation.getCurrentPosition(locationSuccess, locationError, locationOptions);

      } else {
         button.toggleClass("btn-success btn-warning");
         button.prop("disabled", true);
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