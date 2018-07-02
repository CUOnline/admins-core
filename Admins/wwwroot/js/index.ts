((window: any) => {
    let Vue = window.Vue;
    let axios = window.axios;
    let _ = window._;
    let webApiUrl = window.webApiUrl;
    let tokenStr = window.tokenStr;
    let authorized = window.authorized === 'True';

    let app = new Vue({
        el: '#app',
        data: {
            subAccounts: [],
            adminData: [],
            webUrl: webApiUrl,
            loading: true,
            currentProgress: 0
        },
        mounted: async function () {
            if (authorized) {
                // Get the schools
                let response = await axios.get(webApiUrl + '/api/v1/accounts/1/sub_accounts?per_page=100&access_token=' + tokenStr);
                let badValues = [2, 3, 4, 19, 21, 73, 139];
                this.subAccounts = _.filter(response.data, a => badValues.indexOf(a.id) < 0);

                // Get the admins for those schools
                for (let i = 0; i < this.subAccounts.length; ++i) {
                    let adminsResponse = await axios.get(webApiUrl + '/api/v1/accounts/' + this.subAccounts[i].id + '/admins?access_token=' + tokenStr);
                    this.subAccounts[i].admins = adminsResponse.data;
                }

                for (let i = 0; i < this.subAccounts.length; ++i) {
                    for (let j = 0; j < this.subAccounts[i].admins.length; ++j) {
                        var admin = this.subAccounts[i].admins[j];
                        if (!admin.email || admin.email === '') {
                            var existingAdminData = _.find(this.adminData, function (o) { return o.id === admin.user.id });

                            if (existingAdminData) {
                                this.subAccounts[i].admins[j].email = existingAdminData.email;
                                this.subAccounts[i].admins[j].avatarUrl = existingAdminData.avatarUrl;
                                this.subAccounts[i].admins[j].lastActivity = existingAdminData.lastActivity;
                            }
                            else {
                                let profileResponse = await axios.get(webApiUrl + '/api/v1/users/' + admin.user.id + '/profile?access_token=' + tokenStr);
                                this.subAccounts[i].admins[j].email = profileResponse.data.primary_email;
                                this.subAccounts[i].admins[j].avatarUrl = profileResponse.data.avatar_url;

                                let activityResponse = await axios.get(webApiUrl + '/api/v1/users/' + admin.user.id + '/page_views?per_page=1&access_token=' + tokenStr)
                                this.subAccounts[i].admins[j].lastActivity = activityResponse.data.updated_at;

                                this.adminData.push({
                                    id: admin.user.id,
                                    email: profileResponse.data.primary_email,
                                    avatarUrl: profileResponse.data.avatar_url,
                                    lastActivity: activityResponse.data.updated_at
                                });
                            }

                            this.$forceUpdate();
                        }
                    }


                    this.currentProgress = (i / this.subAccounts.length) * 100;
                }
            }

            this.loading = false;
        },
        filters: {
            'formatDate': function (value) {
                if (!value) return '';
                let date = new Date(value);

                // get time
                let hours = date.getHours();
                let minutes = date.getMinutes();
                hours = hours % 12;
                hours = hours ? hours : 12;

                let strTime = `${hours}:${minutes < 10 ? '0' + minutes : minutes}${hours >= 12 ? 'pm' : 'am'}`;
                return `${date.getMonth() + 1}-${date.getDate()}-${date.getFullYear()} ${strTime}`;
            }
        }
    });
})(window);