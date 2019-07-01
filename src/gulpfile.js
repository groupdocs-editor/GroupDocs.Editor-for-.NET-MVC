var gulp = require('gulp')
gulp.task('build', function () {
    return gulp
        .src('./node_modules/@groupdocs.examples.jquery/**')
        .pipe(gulp.dest('./Resources/'))   
})

gulp.task('copy', function () {
    return gulp
        .src('./node_modules/@groupdocs.examples.angular/**')     
        .pipe(gulp.dest('./Resources/angular'))
})