var controls = {
   setPage: function (page) {
      $('#id_page').val(page);
   },
   submit: function (page) {
      controls.setPage(page);
      $("#id_report").submit();
   },
   setSize: function (size) {
      $('#id_size').val(size);
      controls.submit(1);
   },
   setSort: function (sort) {
      $('#id_sort').val(sort);
   },
   lastChecked: null,
   bulkActionLength: 0
}

// https://stackoverflow.com/questions/1634748/how-can-i-delete-a-query-string-parameter-in-javascript
function removeUrlParameter(url, parameter) {
   //prefer to use l.search if you have a location/link object
   var urlparts = url.split('?');
   if (urlparts.length >= 2) {

      var prefix = encodeURIComponent(parameter) + '=';
      var pars = urlparts[1].split(/[&;]/g);

      //reverse iteration as may be destructive
      for (var i = pars.length; i-- > 0;) {
         //idiom for string.startsWith
         if (pars[i].lastIndexOf(prefix, 0) !== -1) {
            pars.splice(i, 1);
         }
      }

      url = urlparts[0] + (pars.length > 0 ? '?' + pars.join('&') : "");
      return url;
   } else {
      return url;
   }
}

function getUrlParameter(name) {
   let regex = new RegExp('[?&]' + name + '=([^&#]*)');
   let results = regex.exec(window.location.search);
   return results ? decodeURIComponent(results[1]) : null;
}

function bulkAction(page, name) {
   var length = $('.bulk-action:checked').length;
   if (length > 0) {

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

      var $form = $('#id_report');
      $form.attr('method', 'POST');
      $form.attr('action', server.bulkActionUrl);
      $form.append($(".AntiForge").html());
      $form.append('<input type="hidden" name="ActionName" value="' + name + '" />');
      $form.append('<input type="hidden" name="ActionCount" value="' + controls.bulkActionLength + '" />');
      $form.append('<input type="hidden" name="ReturnUrl" value="' + window.location.href + '" />');
      controls.submit(page);
   }
}

function updateBulkActions() {
   var length = $(".bulk-action:checked").length;
   var all = length === $(".bulk-action").length;
   $(".bulk-action-link").each(function () {
      var link = $(this);
      var len = (all ? "All" : $('#select-all:checked').length > 0 ? length - 1 : length);
      controls.bulkActionLength = len;
      link.html(link.attr('rel') + ' <span class="badge text-bg-dark">' + len + "</span>");
   });
}

function edit(action) {
   const fields = [];

   // Collect selected fields and ensure they are integers
   $(".field-check:checked").each(function () {
      fields.push($(this).data("src"));
   });

   console.log(action + " fields:", fields.join("."));

   // Get existing comma-delimited values from form fields
   let hide = $('#id_h').val().split(".").filter(Boolean);
   let search = $('#id_s').val().split(".").filter(Boolean);
   let facet = $('#id_f1').val().split(".").filter(Boolean);
   let facets = $('#id_f2').val().split(".").filter(Boolean);
   let timeAgo = $('#id_ta').val().split(".").filter(Boolean);
   let ellipse = $('#id_e').val().split(".").filter(Boolean);
   let order = $('#id_o').val().split(".").filter(Boolean);

   if (action === 'hide') {
      hide = _.union(hide, fields);

      // hide them on client first
      hide.forEach(function (index) {
         // find matching column
         var columnPosition = $("th[data-src='" + index + "']").index() + 1;

         if (columnPosition > 0) { // ensure it exists
            // hide the header
            $("th[data-src='" + index + "']").hide();

            // gide the corresponding cells using the actual position
            $("tr").each(function () {
               $(this).find("td:nth-child(" + columnPosition + ")").hide();
            });
         }
      });

      // remove hidden fields from search, facet, facets, and timeAgo
      // search = _.difference(search, hide);
      // facet = _.difference(facet, hide);
      // facets = _.difference(facets, hide);
      timeAgo = _.difference(timeAgo, hide);
      ellipse = _.difference(ellipse, hide);
   } else if (action === 'search') {
      search = _.union(search, fields);
      facet = _.difference(facet, search);
      facets = _.difference(facets, search);
   } else if (action === 'facet') {
      facet = _.union(facet, fields);
      search = _.difference(search, facet);
      facets = _.difference(facets, facet);
   } else if (action === 'facets') {
      facets = _.union(facets, fields);
      search = _.difference(search, facets);
      facet = _.difference(facet, facets);
   } else if (action === 'clear') {
      // Clear search, facet, and facets for the specific column(s)
      search = _.difference(search, fields);
      facet = _.difference(facet, fields);
      facets = _.difference(facets, fields);
      timeAgo = _.difference(timeAgo, fields);
   } else if (action === 'timeago') {
      timeAgo = _.union(timeAgo, fields);
   } else if (action === 'ellipsis') {
      ellipse = _.union(ellipse, fields);
   } else if (action === 'order') {
      order = _.difference(order, hide);
   }

   // Update form fields with updated comma-delimited lists
   $('#id_h').val(hide.join("."));
   $('#id_s').val(search.join("."));
   $('#id_f1').val(facet.join("."));
   $('#id_f2').val(facets.join("."));
   $('#id_ta').val(timeAgo.join("."));
   $('#id_e').val(ellipse.join("."));
   $('#id_o').val(order.join("."));

   console.log({
      hide: hide,
      search: search,
      facet: facet,
      facets: facets,
      timeAgo: timeAgo,
      ellipse: ellipse,
      order: order
   });

   if (action !== 'order' && action !== 'hide') {
      controls.submit(server.entity.page === 0 ? 0 : 1);
   }
}

function setColumnOrder() {
   var indexString = $("th[data-src]").map(function () {
      return $(this).attr("data-src");
   }).get().join(".");
   $('#id_o').val(indexString);
   console.log("column order: " + indexString);
}

$(document).ready(function () {

   var cleared = "_Cleared";
   var lastFilter;

   var $boxes = $('.shift-select');
   $boxes.click(function (e) {
      if (!controls.lastChecked) {
         controls.lastChecked = this;
         return;
      }

      if (e.shiftKey) {
         var start = $boxes.index(this);
         var end = $boxes.index(controls.lastChecked);

         $boxes.slice(Math.min(start, end), Math.max(start, end) + 1).prop('checked', controls.lastChecked.checked);
      }

      controls.lastChecked = this;
   });

   $('.field-search').on('keypress', function (e) {
      if (e.keyCode === 13) { // Enter key pressed
         e.preventDefault(); // prevent form submission

         let inputValue = $(this).val(); // Get the current input value

         if (inputValue !== "" && inputValue !== "*") {
            $('#id_last').val($(this).attr('name'));
         }
         controls.submit(1);

      }
   });

   $('#id_report select').selectpicker({
      liveSearch: true,
      deselectAllText: "Off",
      noneSelectedText: "All",
      noneResultsText: "Not Found",
      selectAllText: "On",
      selectedTextFormat: "count > 2",
      style: "btn-sm btn-light",
      width: "fit",
      sanitize: false,
      showTick: true
   });

   $("#id_report select").on("changed.bs.select", function (e, clickedIndex, isSelected, previousValue) {
      lastFilter = this.name;
      controls.setPage(1);
      if (!this.multiple || $(this).val().length === 0) {
         controls.submit(1);
      }
   });

   $("#id_report select").css("visibility", "visible");

   $(".form-control.date").datepicker({ dateFormat: "yy-mm-dd" });

   $('#id_report').bind('submit', function () {

      // trim white space from text input
      $('input[type="text"]').each(function () {
         this.value = $.trim(this.value);
      });

      // stop double submit
      $('#id_submit').prop('disabled', true);

      // the rest of this just cleans up the URL (bookmark)
      var page = parseInt($('#id_page').val());

      if (page <= 1) {
         $('#id_page').attr('disabled', true);
      }

      $('#id_report input').filter(function () {
         var value = $(this).val();
         return value === "*" || value === "";
      }).attr('disabled', true);

      $("#id_report select").each(function () {
         if (lastFilter !== $(this).attr("name") && lastFilter !== cleared) {
            var selected = $('option:selected', this);
            var count = selected.length;
            if (count === 0) {
               $(this).attr('disabled', true);
            } else if (count === 1) {
               var value = $(selected[0]).val();
               if (value === "" || value === "*") {
                  $(this).attr('disabled', true);
               }
            }
         }
      });

      $('#busy').show();

      // normal submit handler fires
      return true;
   });

   $('#id_clear').click(function () {

      lastFilter = cleared;

      // set single select back to all
      $('#id_report select:not([multiple])').selectpicker('val', '*');

      // set multi-select to none
      $('#id_report select[multiple]').selectpicker('deselectAll');
      $('#id_report select[multiple]').selectpicker('render');

      // set other inputs to blank
      $('#id_report input:visible').val("");

      controls.submit(server.entity.page === 0 ? 0 : 1);
   });

   $('.sortable').click(function () {
      $(this).toggleClass('btn-sort').toggleClass('btn-primary');

      $(this).siblings('.sortable').each(function (i) {
         if ($(this).hasClass('btn-primary')) {
            $(this).removeClass('btn-primary').addClass('btn-sort');
         }
      });

      var sort = '';
      $('td.sorter').each(function (i) {
         var field = $(this).attr('data-src');
         if (field) {
            var index = 0;
            $('a.sortable', $(this)).each(function (j) {
               if ($(this).hasClass('btn-primary')) {
                  switch (index) {
                     case 0:
                        sort += field + 'a.';
                        break;
                     case 1:
                        sort += field + 'd.';
                        break;
                     default:
                        break;
                  }
               }
               index++;
            });
         }
      });
      var expression = sort.replace(/^\.+|\.+$/gm, '');
      console.log(expression);
      controls.setSort(expression);
      controls.submit(server.entity.page === 0 ? 0 : 1);
   });

   $(":checkbox[name=select-all]").click(function () {
      $(":checkbox[name=Records]").prop("checked", this.checked);
      updateBulkActions();
   });

   $(":checkbox[name=Records]").click(function () {
      updateBulkActions();
   });

   $('input[type="text"]').on("focus", function () {
      if ($(this).val() == "*") {
         $(this).select();
      }
   });

   $("table:first").dragtable({
      dragaccept: '.drag',
      persistState: function (table) {
         setColumnOrder();
         edit("order");
      }
   }); 

   // Get the 'last' parameter value from the URL
   let lastValue = getUrlParameter('last');

   // If 'last' has a value, focus on the input element and position the cursor at the end
   if (lastValue) {
      let inputElement = $('input[name="' + lastValue + '"]');
      if (inputElement.length) {
         inputElement.focus(); // Focus on the input element

         // Position the cursor at the end of the text
         let inputElementDom = inputElement[0]; // Get the raw DOM element
         let textLength = inputElementDom.value.length; // Get the length of the current text
         inputElementDom.setSelectionRange(textLength, textLength); // Set the cursor position
      }
   }

});
