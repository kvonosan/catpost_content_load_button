using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ContentLoadButton
{
    /// <summary>
    /// Класс пост вконтакте.
    /// </summary>
    public class Post
    {
        public int id; //идентификатор поста в базе.
        public int group_id; //идентификатор группы вконтакте.
        public int vk_id; //идентификатор поста вконтакте.
        public long date; //дата в которую выложили пост.
        public string text; //текст поста.
        public int likes; //сколько лайков у поста.
        public int reposts; //сколько репостов у поста.
        public int views; //сколько просмотров у поста.
        public string repost_text; //текст, если это репост поста (проверяется если нет основного текста).
        public int owner_id; //идентификатор владельца поста.
        public int who_add; //кто добавил пост в базу(идентификатор пользователя вконтакте).
        public int trash; //если = 1 то пост мусорный и он не показывается.
        public List<Attachment> attachments = new List<Attachment>(); //прикрепления поста (фото, гифки).
    }

    /// <summary>
    /// Класс прикрепление к посту вконтакте.
    /// </summary>
    public class Attachment
    {
        public int id; //идентификатор прикрепления вконтакте в базе.
        public int group_id; //идентификатор группы вконтакте.
        public int post_id; //идентификатор поста в базе.
        public string type; //тип прикрепления: photo или doc.

        //photo
        public int vk_id; //идентификатор прикрепления вконтакте.
        public int owner_id; //идентификатор владельца прикрепления.
        public string link; //ссылка для загрузки(photo или doc).
        public long date; //дата в которую выложили прикрепление.

        //doc проверяется по заголовку(так как могут быть другие doc) mime type = image/gif.
        //vk_id имеется.
        //owner_id имеется.
        //link имеется.
        //date не имеется.
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Post> posts = new List<Post>(); //список постов готовых к постингу.

        public MainWindow()
        {
            InitializeComponent();
            LoadFromSiteBuffer();
        }


        /// <summary>
        /// Функция загрузки постов из буфера в базе данных пользователя с user_id.
        /// Загружаем посты и прикрепления к ним в список posts.
        /// </summary>
        /// <param name="user_id">Идентификатор пользователя (id 38 login kvonosan pass test1234).</param>
        public void LoadFromSiteBuffer(int user_id = 38)
        {
            string connStr = "server=peshkova-natalia.ru;user=root;database=catpost_content_vk;port=3306;password=test1234;Character Set=utf8mb4;";
            MySqlConnection conn = new MySqlConnection(connStr);
            conn.Open();

            string sql_get_buffer = "SELECT post_id FROM user_content WHERE user_id=@user_id";
            MySqlCommand cmd_get_buffer = new MySqlCommand(sql_get_buffer, conn);
            cmd_get_buffer.CommandTimeout = 2147483; //максимальный таймаут ожидания ответа от базы.
            cmd_get_buffer.Prepare();
            cmd_get_buffer.Parameters.Clear();
            cmd_get_buffer.Parameters.AddWithValue("@user_id", user_id);
            MySqlDataReader reader_get_buffer = cmd_get_buffer.ExecuteReader();

            //берем идентификаторы постов из буффера для постинга.
            List<int> posts_ids = new List<int>(); //идентификаторы постов в буфере для постинга.
            while (reader_get_buffer.Read())
            {
                posts_ids.Add(int.Parse(reader_get_buffer["post_id"].ToString()));
            }
            reader_get_buffer.Close();

            //берем сами посты и загружаем в список posts.
            string sql_get_post = "SELECT * FROM posts_vk WHERE trash=0 AND id = @post_id;";
            MySqlCommand cmd_get_post = new MySqlCommand(sql_get_post, conn);
            cmd_get_post.CommandTimeout = 2147483; //максимальный таймаут ожидания ответа от базы.
            cmd_get_post.Prepare();

            //если буфер для постинга не пуст, то загружаем данные всех постов и прикреплений.
            if (posts_ids.Count > 0)
            {
                foreach (var post_id in posts_ids) //перебираем идентификаторы постов в буфере.
                {
                    cmd_get_post.Parameters.Clear();
                    cmd_get_post.Parameters.AddWithValue("@post_id", post_id);
                    var reader_get_post = cmd_get_post.ExecuteReader();

                    //считываем посты.
                    while (reader_get_post.Read())
                    {
                        Post post = new Post();
                        post.id = int.Parse(reader_get_post["id"].ToString());
                        post.group_id = int.Parse(reader_get_post["group_id"].ToString());
                        post.vk_id = int.Parse(reader_get_post["vk_id"].ToString());
                        post.date = int.Parse(reader_get_post["date"].ToString());
                        post.text = reader_get_post["text"].ToString();
                        post.likes = int.Parse(reader_get_post["likes"].ToString());
                        post.reposts = int.Parse(reader_get_post["reposts"].ToString());
                        post.views = int.Parse(reader_get_post["views"].ToString());
                        post.repost_text = reader_get_post["repost_text"].ToString();
                        post.owner_id = int.Parse(reader_get_post["owner_id"].ToString());
                        post.who_add = int.Parse(reader_get_post["who_add"].ToString());
                        post.trash = int.Parse(reader_get_post["trash"].ToString());
                        posts.Add(post);
                    }
                    reader_get_post.Close();

                    foreach (var post in posts) //перебираем загруженные посты
                    {
                        string sql_get_attachments = "SELECT * FROM attachments_vk WHERE post_id = @id";
                        MySqlCommand cmd_get_attachments = new MySqlCommand(sql_get_attachments, conn);
                        cmd_get_attachments.Prepare();
                        cmd_get_attachments.Parameters.Clear();
                        cmd_get_attachments.Parameters.AddWithValue("@id", post.id);
                        MySqlDataReader reader_get_attachments = cmd_get_attachments.ExecuteReader();

                        //список прикреплений из базы.
                        List<Attachment> attachments_from_db = new List<Attachment>();

                        //считываем прикрепления.
                        while (reader_get_attachments.Read())
                        {
                            Attachment attach = new Attachment();
                            attach.id = int.Parse(reader_get_attachments["id"].ToString());
                            attach.group_id = int.Parse(reader_get_attachments["group_id"].ToString());
                            attach.post_id = int.Parse(reader_get_attachments["post_id"].ToString());
                            attach.type = reader_get_attachments["type"].ToString();
                            attach.vk_id = int.Parse(reader_get_attachments["vk_id"].ToString());
                            attach.owner_id = int.Parse(reader_get_attachments["owner_id"].ToString());
                            attach.link = reader_get_attachments["link"].ToString();
                            attach.date = int.Parse(reader_get_attachments["date"].ToString());
                            attachments_from_db.Add(attach);
                        }
                        reader_get_attachments.Close();
                        post.attachments = attachments_from_db;//прикрепляем список прикреплений к посту.
                    }
                }
            }
        }

        /// <summary>
        /// Функция помечает пост как опубликованный(+1 к счетчику публикаций).
        /// </summary>
        /// <param name="user_id">Id пользователя у которого нужно пометить.</param>
        /// <param name="post_id">Id поста у которого нужно пометить.</param>
        public void MarkPublished(int user_id = 1, int post_id = 0)
        {
            string connStr = "server=peshkova-natalia.ru;user=root;database=catpost_content_vk;port=3306;password=test1234;Character Set=utf8mb4;";
            MySqlConnection conn = new MySqlConnection(connStr);
            conn.Open();

            //берем счетчик публикаций из базы.
            string sql_get_buffer = "SELECT published FROM user_content WHERE user_id=@user_id AND post_id=@post_id";
            MySqlCommand cmd_get_buffer = new MySqlCommand(sql_get_buffer, conn);
            cmd_get_buffer.CommandTimeout = 2147483; //максимальный таймаут ожидания ответа от базы.
            cmd_get_buffer.Prepare();
            cmd_get_buffer.Parameters.Clear();
            cmd_get_buffer.Parameters.AddWithValue("@user_id", user_id);
            cmd_get_buffer.Parameters.AddWithValue("@post_id", post_id);
            MySqlDataReader reader_get_buffer = cmd_get_buffer.ExecuteReader();

            int published = 0;
            while (reader_get_buffer.Read())
            {
                published = int.Parse(reader_get_buffer["published"].ToString());
            }
            reader_get_buffer.Close();

            //прибавляем.
            published++;

            //записываем в базу.
            string sql_set_published = "UPDATE user_content SET published=@published WHERE user_id=@user_id AND post_id=@post_id";
            MySqlCommand cmd_set_published = new MySqlCommand(sql_set_published, conn);
            cmd_set_published.CommandTimeout = 2147483; //максимальный таймаут ожидания ответа от базы.
            cmd_set_published.Prepare();
            cmd_set_published.Parameters.Clear();
            cmd_set_published.Parameters.AddWithValue("@user_id", user_id);
            cmd_set_published.Parameters.AddWithValue("@post_id", post_id);
            cmd_set_published.Parameters.AddWithValue("@published", published);
            cmd_set_published.ExecuteNonQuery();
        }

        /// <summary>
        /// Функция удаления поста из списка по индексу.
        /// </summary>
        /// <param name="post_index">Индес поста.</param>
        public void DeletePostFromListByIndex(int post_index)
        {
            if (posts.Count > post_index && post_index > -1)
            {
                posts.RemoveAt(post_index);
            }
        }

        /// <summary>
        /// Функция удаления поста из списка по идентификатору.
        /// </summary>
        /// <param name="post_id">Идентификатор поста.</param>
        public void DeletePostFromListById(int post_id)
        {
            int count = 0;
            foreach (var post in posts.ToArray())
            {
                if (post.id == post_id)
                {
                    posts.RemoveAt(count);
                }
                count++;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            box.Clear();
            box.AppendText("ВКОНТАКТЕ" + Environment.NewLine + Environment.NewLine);
            if (posts.Count > 0)
            {
                box.AppendText("Текст поста:" + Environment.NewLine);
                string text = "Пост без текста.";
                if (posts[0].text != "")
                {
                    text = posts[0].text;
                } else if (posts[0].repost_text != "")
                {
                    text = posts[0].repost_text;
                }
                box.AppendText(text + Environment.NewLine + Environment.NewLine);
                box.AppendText("Прикрепления(ссылки):" + Environment.NewLine);
                foreach (var attach in posts[0].attachments)
                {
                    box.AppendText(attach.link + Environment.NewLine);
                }
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            box.Clear();
            box.AppendText("ФЕЙСБУК" + Environment.NewLine + Environment.NewLine);
            if (posts.Count > 0)
            {
                box.AppendText("Текст поста:" + Environment.NewLine);
                string text = "Пост без текста.";
                if (posts[0].text != "")
                {
                    text = posts[0].text;
                }
                else if (posts[0].repost_text != "")
                {
                    text = posts[0].repost_text;
                }
                box.AppendText(text + Environment.NewLine + Environment.NewLine);
                box.AppendText("Прикрепления(ссылки):" + Environment.NewLine);
                foreach (var attach in posts[0].attachments)
                {
                    box.AppendText(attach.link + Environment.NewLine);
                }
            }
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            box.Clear();
            box.AppendText("ОДНОКЛАССНИКИ" + Environment.NewLine + Environment.NewLine);
            if (posts.Count > 0)
            {
                box.AppendText("Текст поста:" + Environment.NewLine);
                string text = "Пост без текста.";
                if (posts[0].text != "")
                {
                    text = posts[0].text;
                }
                else if (posts[0].repost_text != "")
                {
                    text = posts[0].repost_text;
                }
                box.AppendText(text + Environment.NewLine + Environment.NewLine);
                box.AppendText("Прикрепления(ссылки):" + Environment.NewLine);
                foreach (var attach in posts[0].attachments)
                {
                    box.AppendText(attach.link + Environment.NewLine);
                }
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            box.Clear();
            box.AppendText("ИНСТАГРАММ" + Environment.NewLine + Environment.NewLine);
            if (posts.Count > 0)
            {
                box.AppendText("Текст поста:" + Environment.NewLine);
                string text = "Пост без текста.";
                if (posts[0].text != "")
                {
                    text = posts[0].text;
                }
                else if (posts[0].repost_text != "")
                {
                    text = posts[0].repost_text;
                }
                box.AppendText(text + Environment.NewLine + Environment.NewLine);
                box.AppendText("Прикрепления(ссылки):" + Environment.NewLine);
                foreach (var attach in posts[0].attachments)
                {
                    box.AppendText(attach.link + Environment.NewLine);
                }
            }
        }
    }
}
