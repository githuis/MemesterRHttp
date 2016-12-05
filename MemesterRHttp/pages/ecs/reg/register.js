        var $status = $('#status');

        function register() {
            $status.text("");
            var usr = $('#usr').val();
            var pwd1 = $('#pwd1').val();
            var pwd2 = $('#pwd2').val();

            if (!checkInput(usr, pwd1, pwd2))
            {
                $status.text("Invalid input");
                return;
            }
            console.log("sending");
            $.post("/register", {
                username: usr,
                password: pwd1
            }, function (data) {
                alert(data);
                if (data == "ok")
                    $status.text("Registered!");
                else
                    $status.text("Not registered!");
            });
            console.log("sent");
        }

        function checkInput(usr, pwd1, pwd2) {
            return !(usr.length < 4 || usr.length > 20 || pwd1.length < 3 || pwd1.length > 20 || pwd1 != pwd2);
        }

        $("#submit").click(register);