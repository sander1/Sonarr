var gulp = require('gulp');
var webpack = require('gulp-webpack');
var paths = require('./paths');

require('./handlebars.js');


gulp.task('webpack', ['handlebars'], function (cb) {


 return gulp.src('src/UI/app.js')
 .pipe(webpack({
    resolve:{
        root: 'src/UI',
        alias:{
           'backbone'                : 'JsLibraries/backbone',
           'moment'                  : 'JsLibraries/moment',
           'filesize'                : 'JsLibraries/filesize',
           'handlebars'              : 'Shared/Shims/handlebars',
           'handlebars.helpers'      : 'JsLibraries/handlebars.helpers',
           'bootstrap'               : 'JsLibraries/bootstrap',
           'backbone.deepmodel'      : 'JsLibraries/backbone.deep.model',
           'backbone.pageable'       : 'JsLibraries/backbone.pageable',
           'backbone.validation'     : 'JsLibraries/backbone.validation',
           'backbone.modelbinder'    : 'JsLibraries/backbone.modelbinder',
           'backbone.collectionview' : 'JsLibraries/backbone.collectionview',
           'backgrid'                : 'JsLibraries/backbone.backgrid',
           'backgrid.paginator'      : 'JsLibraries/backbone.backgrid.paginator',
           'backgrid.selectall'      : 'JsLibraries/backbone.backgrid.selectall',
           'fullcalendar'            : 'JsLibraries/fullcalendar',
           'backstrech'              : 'JsLibraries/jquery.backstretch',
           'underscore'              : 'JsLibraries/lodash.underscore',
           'marionette'              : 'JsLibraries/backbone.marionette',
           'signalR'                 : 'JsLibraries/jquery.signalR',
           'jquery-ui'               : 'JsLibraries/jquery-ui',
           'jquery.knob'             : 'JsLibraries/jquery.knob',
           'jquery.easypiechart'     : 'JsLibraries/jquery.easypiechart',
           'jquery.dotdotdot'        : 'JsLibraries/jquery.dotdotdot',
           'messenger'               : 'JsLibraries/messenger',
           'jquery'                  : 'JsLibraries/jquery',
           'typeahead'               : 'JsLibraries/typeahead',
           'zero.clipboard'          : 'JsLibraries/zero.clipboard',
           'libs'                    : 'JsLibraries/'
       }
   }
}))
.pipe(gulp.dest('dist/'));
/*
    var config = {
        mainConfigFile: 'src/UI/app.js',
        fileExclusionRegExp: /^.*\.(?!js$)[^.]+$/,
        preserveLicenseComments: false,
        dir: paths.dest.root,
        optimize: 'none',
        removeCombined: true,
        inlineText: false,
        keepBuildDir: true,
        modules: [
            {
                name: 'app',
                exclude: ['templates.js']
            }
        ]};

    requirejs.optimize(config, function (buildResponse) {
        console.log(buildResponse);
        cb();
    });*/

});
