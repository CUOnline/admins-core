var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
((window) => {
    let Vue = window.Vue;
    let axios = window.axios;
    let _ = window._;
    let webApiUrl = window.webApiUrl;
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
        mounted: function () {
            return __awaiter(this, void 0, void 0, function* () {
                if (authorized) {
                    // Get the schools
                    let response = yield axios.get('/Admins/Canvas/GetSubAccounts');
                    let badValues = [2, 3, 4, 19, 21, 73, 139];
                    this.subAccounts = _.filter(response.data, a => badValues.indexOf(a.id) < 0);
                    this.currentProgress = 25;
                    // Get the admins for those schools
                    for (let i = 0; i < this.subAccounts.length; ++i) {
                        let adminsResponse = yield axios.get('/Admins/Canvas/GetAdmins?subAccountId=' + this.subAccounts[i].id);
                        let admins = _.orderBy(adminsResponse.data, 'user.name', 'asc');
                        // combine duplicate admins
                        for (var k = 0; k < admins.length; ++k) {
                            for (var j = k + 1; j < admins.length; ++j) {
                                if (admins[k].user.id === admins[j].user.id) {
                                    admins[k].role += ', ' + admins[j].role;
                                    admins.splice(j--, 1);
                                }
                            }
                        }
                        ;
                        this.subAccounts[i].admins = admins;
                    }
                    this.currentProgress = 50;
                    var uniqueUsers = _.uniqBy(_.map(_.flatten(_.map(this.subAccounts, 'admins')), 'user'), 'id');
                    // build requests
                    var userDataRequests = [];
                    for (let i = 0; i < uniqueUsers.length; ++i) {
                        userDataRequests.push('/Admins/Canvas/GetUserProfile?userId=' + uniqueUsers[i].id);
                        userDataRequests.push('/Admins/Canvas/GetUserPageViews?userId=' + uniqueUsers[i].id);
                    }
                    // chunk requests so we don't bombard the server
                    var requests = _.chunk(userDataRequests, 20);
                    var numRequests = requests.length;
                    var result = [];
                    for (let i = 0; i < requests.length; ++i) {
                        var curResult = yield axios.all(requests[i].map(l => axios.get(l)));
                        result = result.concat(curResult);
                        this.$forceUpdate();
                        this.currentProgress = 50 + ((i / requests.length) * 50);
                    }
                    var userProfiles = result.filter(x => x.data.isUserProfile).map(x => JSON.parse(x.data.data));
                    var activities = result.filter(x => !x.data.isUserProfile).map(x => JSON.parse(x.data.data));
                    // parse responses
                    for (let i = 0; i < uniqueUsers.length; ++i) {
                        var userProfile = userProfiles.filter(el => el.id == uniqueUsers[i].id)[0];
                        var lastActivity = activities.filter(el => el.length > 0 && el[0].links.user == uniqueUsers[i].id);
                        this.adminData.push({
                            id: uniqueUsers[i].id,
                            email: userProfile.primary_email,
                            avatarUrl: userProfile.avatar_url,
                            lastActivity: lastActivity.length > 0 ? lastActivity[0][0].updated_at : ''
                        });
                    }
                    // parse data and add information to admin list
                    for (let i = 0; i < this.subAccounts.length; ++i) {
                        for (let j = 0; j < this.subAccounts[i].admins.length; ++j) {
                            var admin = this.subAccounts[i].admins[j];
                            if (!admin.email || admin.email === '') {
                                var existingAdminData = _.find(this.adminData, function (o) { return o.id === admin.user.id; });
                                if (existingAdminData) {
                                    this.subAccounts[i].admins[j].email = existingAdminData.email;
                                    this.subAccounts[i].admins[j].avatarUrl = existingAdminData.avatarUrl;
                                    this.subAccounts[i].admins[j].lastActivity = existingAdminData.lastActivity;
                                }
                            }
                        }
                    }
                }
                this.loading = false;
            });
        },
        methods: {
            CombineDuplicateAdmins: function (admins) {
                return admins;
            }
        },
        filters: {
            'formatDate': function (value) {
                if (!value)
                    return '';
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
