
var config = require('../config');
var gulp = require('gulp');

var eslint = require('gulp-eslint');
var babel = require("gulp-babel");
var sort = require('gulp-sort');
var concat = require('gulp-concat');
var wrap = require("gulp-wrap-js");
var embedTemplates = require('gulp-angular-embed-templates');
var _ = require('lodash');

module.exports = function (files, out) {
    
    _.forEach(config.roots, function(root){
        console.log("JS: ", files, " -> ", root + config.targets.js + out)
    })
    
    var task = gulp.src(files);

    // check for js errors
    task = task.pipe(eslint());
    // outputs the lint results to the console
    task = task.pipe(eslint.format());

    // sort files in stream by path or any custom sort comparator
    task = task.pipe(babel())
        .pipe(sort());
    
    //in production, embed the templates
    task = task.pipe(embedTemplates({ basePath: "./src/", minimize: { loose: true } }))
    
    task = task.pipe(concat(out)).pipe(wrap('(function(){\n%= body %\n})();'))

    _.forEach(config.roots, function(root){
        task = task.pipe(gulp.dest(root + config.targets.js));
    })
        


    return task;

};
