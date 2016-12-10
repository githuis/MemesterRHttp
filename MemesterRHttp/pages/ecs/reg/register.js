        var $status = $('#status');
        var $submit = $("#submit");

        String.prototype.hashCode = function() {
            var hash = 0;
            var len = this.length;
            if (len == 0) return hash;
            for (var i = 0; i < len; i++){
                var ch = this.charCodeAt(i);
                hash = ((hash<<5)-hash)+ch;
                hash = hash & hash;
            }
            return hash;
        };


        function register() {
            $status.text("");
            var usr = $('#usr').val();
            var pwd1 = $('#pwd1').val();
            var pwd2 = $('#pwd2').val();

            if (!checkInput(usr, pwd1, pwd2))
            {
                return;
            }
            $.post("/register", {
                username: usr,
                password: pwd1.hashCode()
            }, function (data) {
                alert(data);
                if (data == "ok")
                {
                    $status.text("Registered!");
                    sessionStorage.setItem("usr", usr);
                    sessionStorage.setItem("pwd", pwd);
                    window.location.href = "/";
                }
                else
                    $status.text("Username already taken");
            });
        }

        function checkInput(usr, pwd1, pwd2) {
            if (usr.length < 4){
                $status.text("Username too short (min. 4 characters");
                return false;
            }
            if (usr.length > 20){
                $status.text("Username too long (max. 20 characters");
                return false;
            }
            if (pwd1.length < 3){
                $status.text("Password too short (min. 3 characters");
                return false;
            }
            if (pwd1.length > 20){
                $status.text("Password too long (max. 20 characters");
                return false;
            }
            if (pwd1 != pwd2){
                $status.text("Passwords do not match");
                return false;
            }
            var usernameRegex = /^[a-zA-Z0-9]+$/;
            if (!validateName(usernameRegex.test(name))){
                $status.text("Username contains invalid characters");
                return false;
            }
            return true;
        }
        $submit.click(register);