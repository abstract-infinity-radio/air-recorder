// Template for webpack.config.js in Fable projects
// In most cases, you'll only need to edit the CONFIG object (after dependencies)
// See below if you need better fine-tuning of Webpack options

// Dependencies. Also required: core-js, @babel/core,
// @babel/preset-env, babel-loader, sass, sass-loader, css-loader, style-loader, file-loader
var path = require("path");
var webpack = require("webpack");
var HtmlWebpackPlugin = require('html-webpack-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');
var MiniCssExtractPlugin = require("mini-css-extract-plugin");
var Dotenv = require('dotenv-webpack');
var realFs = require('fs');
var gracefulFs = require('graceful-fs');

gracefulFs.gracefulify(realFs);

var mode = process.env.NODE_ENV
mode = mode ? mode : "production"
// If we're running the webpack-dev-server, assume we're in development mode
const isProduction = mode === 'production'
const isDevelopment = !isProduction

var CONFIG = {
    // The tags to include the generated JS and CSS will be automatically injected in the HTML template
    // See https://github.com/jantimon/html-webpack-plugin
    indexHtmlTemplate: './src/AirRecorder.Client/index.html',
    fsharpEntry: './.fable-build/App.js',
    cssEntry: './src/AirRecorder.Client/styles/main.sass',
    outputDir: './publish/app/public',
    assetsDir: './src/AirRecorder.Client/public',
    devServerPort: 8080,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: {
        '/api/**': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || "5000"),
            changeOrigin: true
        },
        '/download/**': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || "5000"),
            changeOrigin: true
        },
        '/socket/**': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || "5000"),
            ws: true
        }
    }
}


console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
var commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve(CONFIG.indexHtmlTemplate)
    }),

    new Dotenv({
        path: "./.env",
        silent: false,
        systemvars: true
    })
];

module.exports = {
    // In development, bundle styles together with the code so they can also
    // trigger hot reloads. In production, put them in a separate CSS file.
    entry:  {
        app: [resolve(CONFIG.fsharpEntry), resolve(CONFIG.cssEntry)]
    },
    // Add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: {
        path: resolve(CONFIG.outputDir),
        publicPath: '/',
        filename: isProduction ? '[name].[fullhash].js' : '[name].js'
    },
    mode: mode,
    devtool: isProduction ? "source-map" : "eval-source-map",
    optimization: {
        runtimeChunk: "single",
        moduleIds: 'deterministic',
        // Split the code coming from npm packages into a different file.
        // 3rd party dependencies change less often, let the browser cache them.
        splitChunks: {
            cacheGroups: {
                commons: {
                    test: /node_modules/,
                    name: "vendors",
                    chunks: "all",
                    enforce: true
                }
            }
        },
    },
    // Besides the HtmlPlugin, we use the following plugins:
    // PRODUCTION
    //      - MiniCssExtractPlugin: Extracts CSS from bundle to a different file
    //          To minify CSS, see https://github.com/webpack-contrib/mini-css-extract-plugin#minimizing-for-production
    //      - CopyWebpackPlugin: Copies static assets to output directory
    // DEVELOPMENT
    //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
    plugins: isProduction ?
        commonPlugins.concat([
            new MiniCssExtractPlugin({ filename: '[name].[chunkhash].css' }),
            new CopyWebpackPlugin({
                patterns: [
                    { from: resolve(CONFIG.assetsDir) }
                ]
            }),
        ])
        : commonPlugins.concat([
            new webpack.HotModuleReplacementPlugin(),
        ]),
    // Configuration for webpack-dev-server
    devServer: {
        static: {
            directory: resolve(CONFIG.assetsDir),
            publicPath: '/'
        },
        host: '0.0.0.0',
        port: CONFIG.devServerPort,
        proxy: CONFIG.devServerProxy,
        historyApiFallback: true
    },
    module: {
        rules: [
            {
                test: /\.js$/,
                enforce: "pre",
                use: ['source-map-loader'],
            },
            {
                test: /\.(sass|scss|css)$/,
                use: [
                    isProduction
                        ? MiniCssExtractPlugin.loader
                        : 'style-loader',
                    'css-loader',
                    'resolve-url-loader',
                    'postcss-loader',
                    'sass-loader',
                ],
            }
        ]
    }
};

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
