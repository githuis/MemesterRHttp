function register() {
    var usr = $('#usr').text();
    var pwd1 = $('#pwd1').text();
    var pwd2 = $('#pwd2').text();

    if (!checkInput(usr, pwd1, pwd2))
        return

    $.ajax("memester.club/register", )
}

function checkInput(usr, pwd1, pwd2) {

    if (usr.length < 4 || usr.length > 20){
        return false;
    }
    if (pwd1.length < 3 || pwd1 > 20 || pwd1 != pwd2){
        return false;
    }




}