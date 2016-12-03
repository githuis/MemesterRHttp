        var $status = $('#status');

        function register() {
            $status.text("");
            var usr = $('#usr').text();
            var pwd1 = $('#pwd1').text();
            var pwd2 = $('#pwd2').text();

            if (!checkInput(usr, pwd1, pwd2))
            {
                $status.text("Invalid input");
                return;
            }

            $.ajax({
                type: "POST",
                url: "/register",
                data: {
                    username: usr,
                    password: pwd1
                },
                success: registered,
                error: notRegistered,
                dataType: "x-www-form-urlencoded"
            });
        }

        function registered(data, text, jqXHR){
            alert(data);
            if (data == "ok")
                $status.text("Registered!");
            else
                $status.text("Not registered!");
        }

        function notRegistered(jqXHR, status, error) {
            $status.text("Not registered!");
        }

        function checkInput(usr, pwd1, pwd2) {
            return !(usr.length < 4 || usr.length > 20 || pwd1.length < 3 || pwd1.length > 20 || pwd1 != pwd2);
        }

        $("submit").click(register);