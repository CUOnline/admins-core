/// <binding BeforeBuild='scripts, compileTS' />
var gulp = require('gulp');
var merge = require('merge-stream'); 
var ts = require("gulp-typescript");

var tsProject = ts.createProject("tsconfig.json");

// Dependency Dirs
var deps = {
    "jquery": {
        "dist/*": ""
    },
    "bootstrap": {
        "dist/**/*": ""
    },
    "bootstrap-vue": {
        "dist/*": ""
    },
    "popper.js": {
        "dist/**/*": ""
    },
    "lodash": {
        "lodash*.*": ""
    },
    "vue": {
        "dist/*": ""
    },
    "vee-validate": {
        "dist/**/*": ""
    },
    "vue-resource": {
        "dist/*": ""
    },
    "axios": {
        "dist/*": ""
    }
};

gulp.task("scripts", function () {

    var streams = [];

    for (var prop in deps) {
        console.log("Prepping Scripts for: " + prop);
        for (var itemProp in deps[prop]) {
            streams.push(gulp.src("node_modules/" + prop + "/" + itemProp)
                .pipe(gulp.dest("wwwroot/lib/" + prop + "/" + deps[prop][itemProp])));
        }
    }

    return merge(streams);

});

gulp.task("compileTS", function () {
    return tsProject.src()
        .pipe(tsProject())
        .js.pipe(gulp.dest("wwwroot/js"));
});