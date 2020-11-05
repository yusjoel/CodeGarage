package com.joel.unitywhatsnewgraber;

import android.app.Activity;
import android.content.pm.PackageManager;
import android.os.Bundle;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.google.android.material.snackbar.Snackbar;

import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;
import androidx.core.app.ActivityCompat;

import android.os.Environment;
import android.os.StrictMode;
import android.util.Log;
import android.view.View;

import android.view.Menu;
import android.view.MenuItem;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.ArrayList;
import java.util.List;

public class MainActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);

        MainActivity.verifyStoragePermissions(this);

        FloatingActionButton fab = findViewById(R.id.fab);
        fab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                initializeUrls();
                downloadWhatsNew();
                //downloadFiles();
            }
        });
    }

    private void initializeUrls() {
        try {
            MainActivity.this.openUrl("https://unity3d.com/unity/whats-new/2020.1.3", "whats-new/index.html");
            File file = new File(Environment.getExternalStorageDirectory(), "whats-new/index.html");
            if(file.exists()) {
                BufferedReader br = new BufferedReader(new FileReader(file));
                String line;
                boolean optionsStart = false;
                while((line = br.readLine()) != null) {
                    if(!optionsStart) {
                        if (line.contains("<ul class=\"options\">")) {
                            optionsStart = true;
                        }
                    } else {
                        if(line.contains("Archive")) {
                            break;
                        }
                        //System.out.println(line);
                        int index1 = line.indexOf("href=\"") + "href=\"".length();
                        int index2 = line.indexOf("\">Unity");
                        //System.out.println(index1);
                        //System.out.println(index2);
                        String url = "https://unity3d.com" + line.substring(index1, index2);
                        whatsNewUrls.add(url);
                        //System.out.println(url);
                    }
                }

                System.out.println("found versions: " + whatsNewUrls.size());
            } else {
                System.out.println("download failed");
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void downloadFiles() {
        try {
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/css/unity/unity-core.css", "themes/unity/css/unity/unity-core.css");
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/css/unity/unity-helpers.css", "themes/unity/css/unity/unity-helpers.css");
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/assets/layout/core-sprites.png", "themes/unity/images/assets/layout/core-sprites.png");
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/ui/favicons/apple-touch-icon-152x152.png", "themes/unity/images/ui/favicons/apple-touch-icon-152x152.png");
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/ui/favicons/favicon.ico", "themes/unity/images/ui/favicons/favicon.ico");

            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/ui/ui/unity-logo-white.svg", "themes/unity/images/ui/ui/unity-logo-white.svg");
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/ui/sprites/core-sprite.png", "themes/unity/images/ui/sprites/core-sprite.png");
            //MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/ui/sprites/core-sprite-wh.png", "themes/unity/images/ui/sprites/core-sprite-wh.png");

            MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/js/core.js", "themes/unity/js/core.js");
            MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/css/core.css", "themes/unity/css/core.css");
            MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/css/custom.css", "themes/unity/css/custom.css");
            MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/css/responsive-core.css", "themes/unity/css/responsive-core.css");
            MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/assets/favicons/apple-touch-icon-152x152.png", "themes/unity/images/assets/favicons/apple-touch-icon-152x152.png");
            MainActivity.this.openUrl("https://unity3d.com/profiles/unity3d/themes/unity/images/assets/favicons/favicon.ico", "themes/unity/images/assets/favicons/favicon.ico");

            MainActivity.this.openUrl("https://unity3d.com/utils/cp.js", "themes/utils/cp.js");
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void downloadWhatsNew() {
        try {
            for (int i = 0; i < MainActivity.whatsNewUrls.size(); i++) {
                System.out.println((i + 1) + "/" + MainActivity.whatsNewUrls.size());

                String url = MainActivity.whatsNewUrls.get(i);
                int lastIndex = url.lastIndexOf('/');
                String fileName = "whats-new" + url.substring(lastIndex) + ".html";
                MainActivity.this.openUrl(url, fileName);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    //先定义
    private static final int REQUEST_EXTERNAL_STORAGE = 1;

    private static String[] PERMISSIONS_STORAGE = {
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.WRITE_EXTERNAL_STORAGE" };



    private static List<String> whatsNewUrls = new ArrayList<String>();

    public static void verifyStoragePermissions(Activity activity) {
        try {
            //检测是否有写的权限
            int permission = ActivityCompat.checkSelfPermission(activity,
                    "android.permission.WRITE_EXTERNAL_STORAGE");
            if (permission != PackageManager.PERMISSION_GRANTED) {
                // 没有写的权限，去申请写的权限，会弹出对话框
                ActivityCompat.requestPermissions(activity, PERMISSIONS_STORAGE,REQUEST_EXTERNAL_STORAGE);
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    private void openUrl(String requestUrl, String fileName) throws IOException {
        File file = new File(Environment.getExternalStorageDirectory(), fileName);
        if(file.exists()) {
            System.out.println("skip: " + file.getAbsolutePath());
            return;
        }

        String directoryName = fileName.substring(0, fileName.lastIndexOf('/'));
        File directory = new File(Environment.getExternalStorageDirectory(), directoryName);
        directory.mkdirs();

        URL url = new URL(requestUrl);
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
        StrictMode.setThreadPolicy(policy);
        HttpURLConnection urlConn = (HttpURLConnection) url.openConnection();// 设置连接主机超时时间
        urlConn.setConnectTimeout(5 * 1000);//设置从主机读取数据超时
        urlConn.setReadTimeout(5 * 1000);// 设置是否使用缓存  默认是true
        urlConn.setUseCaches(true);// 设置为Post请求
        urlConn.setRequestMethod("GET");//urlConn设置请求头信息
        urlConn.setRequestProperty("Content-Type", "application/json");
        //设置客户端与服务连接类型
        urlConn.addRequestProperty("Connection", "Keep-Alive");
        urlConn.connect();            // 开始连接
        // 判断请求是否成功
        if (urlConn.getResponseCode() == 200) {
            byte[] bytes = streamToString(urlConn.getInputStream());//获取的内容

            FileOutputStream fos = new FileOutputStream(file);
            fos.write(bytes);
            fos.close();

            System.out.println("saved: " + file.getAbsolutePath());
        }
        urlConn.disconnect();
    }

    private static byte[] streamToString(InputStream is) {
        try {
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            byte[] buffer = new byte[1024];
            int len = 0;
            while ((len = is.read(buffer)) != -1) {
                baos.write(buffer, 0, len);
            }
            baos.close();
            is.close();
            byte[] byteArray = baos.toByteArray();
            return byteArray;
        } catch (Exception e) {
            Log.e("Main", e.toString());
            return null;
        }
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }
}