﻿window.fbAsyncInit = function () {
    FB.init({
        appId: '784471912075991',
        cookie: true,
        xfbml: true,
        version: 'v6.0'
    });
};


(function (d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s); js.id = id; js.async = true;
    js.src = "https://connect.facebook.net/en_US/sdk.js";
    fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));


function logOut(e) {
    FB.getLoginStatus(function (response) {
        if (response.status === 'connected') {
            window.location.href = "/SignOut";
        }
    });
}