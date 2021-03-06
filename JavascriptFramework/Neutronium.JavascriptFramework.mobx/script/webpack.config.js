var path = require('path')
var webpack = require('webpack')
const BabiliPlugin = require("babili-webpack-plugin")

var output = {
  path: path.resolve(__dirname, 'dist'),
  filename: 'mobxManager.js',
  library: "mobxManager",
  libraryTarget: "assign"
};

var webpackOptions = {
  output: output,
  module: {
    rules: [
      {
        test: /\.js$/,
        loader: 'babel-loader',
        exclude: /node_modules/
      },
      {
        test: /\.json$/,
        loader: 'json-loader'
      }
    ]
  },
  externals : {
    'mobx': 'mobx'
  },
  devServer: {
    historyApiFallback: true,
    noInfo: true
  },
  resolve: {
    extensions: ['.js', '.json', '.cjson'],
    alias: {
      'src': path.resolve(__dirname, '../src')
    }
  },
  entry: './src/index.js',
  plugins: [
    new webpack.DefinePlugin({
      'process.env': {
        NODE_ENV: '"production"'
      }
    }),
    new webpack.optimize.ModuleConcatenationPlugin()
    //new webpack.LoaderOptionsPlugin({ minimize: true})
  ]
}

if (process.env.NODE_ENV === 'production') {
  webpackOptions.plugins = (webpackOptions.plugins || []).concat([
    new BabiliPlugin({}, { comments: false })
  ]);
}

module.exports = webpackOptions