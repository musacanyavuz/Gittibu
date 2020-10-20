(function($) {
    "use strict";

    $.fn.socialButtons = function(options, socialNetworks) {
        var that = this;
        var options = options || {};
        var socialNetworks = socialNetworks || {};

        that.options = {
            socialNetworks: ["facebook", "twitter", "googleplus"],
            url: "",
            text: "",
            sharelabel: true,
            sharelabelText: "SHARE",
            verticalAlign: false
        };
        $.extend(that.options, options);
        if (that.options.url=="" && $("link[rel=canonical]").length) {
            that.options.url = $("link[rel=canonical]").attr('href');
        } else if (that.options.url=="" && !$("link[rel=canonical]").length && window.location.href!=undefined) {
            that.options.url = window.location.href;
        }
        if (that.options.text=="" && document.title!="") {
            that.options.text = document.title;
        }
        that.options.url = encodeURIComponent(that.options.url);
        that.options.text = encodeURIComponent(that.options.text);

        that.socialNetworks = {
            facebook: {
                title: "Share on Facebook",
                cssclass: "social-facebook",
                shareurl: "https://www.facebook.com/sharer/sharer.php?u="+that.options.url,
                height: 600,
                width: 600
            },
            twitter: {
                title: "Share on Twitter",
                cssclass: "social-twitter",
                shareurl: "http://twitter.com/share?text="+that.options.text+"&url="+that.options.url,
                height: 600,
                width: 600
            },
            googleplus: {
                title: "Share on Google+",
                cssclass: "social-googleplus",
                shareurl: "https://plus.google.com/share?url="+that.options.url,
                height: 600,
                width: 600
            }
        };
        $.each(that.socialNetworks, function(index, value) {
            if (socialNetworks.hasOwnProperty(index)) {
                $.extend(that.socialNetworks[index], socialNetworks[index]);
            }
        });

        if (that.options.socialNetworks.length>0) {
            var container = $(that);
            var verticalAlign = that.options.verticalAlign;

            if (that.options.sharelabel) {
                var classVertical = (verticalAlign) ? " class=\"vertical vertical-label\"" : "";
                var htmlLabel = "<label"+classVertical+">"+that.options.sharelabelText+"</label>";
                container.append(htmlLabel);
            }

            var classVertical = (verticalAlign) ? " class=\"vertical vertical-ul\"" : "";
            var htmlUl = "<ul"+classVertical+"></ul><div></div>";
            container.append(htmlUl);

            $.each(that.options.socialNetworks, function(index, value) {
                if (that.socialNetworks.hasOwnProperty(value)) {
                    var classVertical = (verticalAlign) ? " vertical" : "";

                    $("<li class=\""+that.socialNetworks[value].cssclass+classVertical+"\" title=\""+that.socialNetworks[value].title+"\"></li>").appendTo(container.find("ul")).on("click", function() {
                        window.open(that.socialNetworks[value].shareurl, '', 'menubar=no,toolbar=no,resizeable=no,scrollbars=no,height='+that.socialNetworks[value].height+',width='+that.socialNetworks[value].width+',top='+((screen.height/2)-(that.socialNetworks[value].height/2))+',left='+((screen.width/2)-(that.socialNetworks[value].width/2)));
                    });
                }
            });
        }
    }
})(jQuery);