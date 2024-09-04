using Microsoft.AspNetCore.Components;

namespace WebApp.Components.Pages.Advertisement;

public partial class SamplePage : ComponentBase
{
    #region Parameters

    [SupplyParameterFromQuery(Name = "content_id")]
    public string? ContentId { get; set; }

    #endregion

    #region Fields

    private RenderFragment? PageRenderFragment { get; set; }

    private string Title { get; set; } = string.Empty;
    private List<Dictionary<string, string>> Metadata { get; set; } = [];

    #endregion

    protected override void OnInitialized()
    {
        switch (ContentId)
        {
            case "1":
            {
                Metadata.Clear();
                Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", "First page" } });
                Title = "Page 1";
                RenderPage("<p>This is a <strong>dynamic</strong> content.</p>");

                break;
            }
            case "2":
            {
                Title = "Page 2";
                Metadata.Clear();
                Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", "Second page" } });
                Title = "Page 2";
                RenderPage("""
                            <div class="ocm-effect-wrap">
                           	<div class="ocm-effect-wrap-inner">
                           		<div id="header-space" data-header-mobile-fixed="1" style="height: 100px;"></div>
                           
                           
                           		<div id="header-outer" data-has-menu="true" data-has-buttons="no" data-using-pr-menu="false"
                           			data-mobile-fixed="1" data-ptnm="false" data-lhe="default" data-user-set-bg="#700202"
                           			data-format="default" data-permanent-transparent="false" data-megamenu-rt="1" data-remove-fixed="0"
                           			data-header-resize="1" data-cart="false" data-transparency-option="" data-box-shadow="none"
                           			data-shrink-num="6" data-using-secondary="0" data-using-logo="1" data-logo-height="46"
                           			data-m-logo-height="24" data-padding="27.5" data-full-width="true" data-condense="false">
                           
                           
                           			<div id="search-outer" class="nectar">
                           				<div id="search">
                           					<div class="container">
                           						<div id="search-box">
                           							<div class="inner-wrap">
                           								<div class="col span_12">
                           									<form role="search" action="https://mothership.tv/" method="GET">
                           										<input type="text" name="s" value="" placeholder="Search">
                           
                           										<span>Hit enter to search or ESC to close</span>
                           									</form>
                           								</div><!--/span_12-->
                           							</div><!--/inner-wrap-->
                           						</div><!--/search-box-->
                           						<div id="close"><a href="http://mothership.tv/portfolio/rumours/#">
                           								<span class="close-wrap"> <span class="close-line close-line1"></span> <span
                           										class="close-line close-line2"></span> </span> </a></div>
                           					</div><!--/container-->
                           				</div><!--/search-->
                           			</div><!--/search-outer-->
                           
                           			<header id="top">
                           				<div class="container">
                           					<div class="row">
                           						<div class="col span_3">
                           							<a id="logo" href="https://mothership.tv/" data-supplied-ml-starting-dark="false"
                           								data-supplied-ml-starting="false" data-supplied-ml="false">
                           								<img class="stnd default-logo" alt=""
                           									src="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg"
                           									srcset="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 1x, https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 2x"><img
                           									class="starting-logo default-logo" alt=""
                           									src="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg"
                           									srcset="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 1x, https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 2x"><img
                           									class="starting-logo dark-version default-logo" alt=""
                           									src="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg"
                           									srcset="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 1x, https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 2x">
                           							</a>
                           
                           						</div><!--/span_3-->
                           
                           						<div class="col span_9 col_last">
                           							<div class="slide-out-widget-area-toggle mobile-icon fullscreen-alt"
                           								data-icon-animation="simple-transform">
                           								<div> <a href="http://mothership.tv/portfolio/rumours/#sidewidgetarea"
                           										aria-label="Navigation Menu" aria-expanded="false" class="closed"> <span
                           											aria-hidden="true"> <i class="lines-button x2"> <i class="lines"></i>
                           											</i> <span class="close-wrap"> <span
                           													class="close-line close-line1"></span> <span
                           													class="close-line close-line2"></span> </span></span> </a>
                           								</div>
                           							</div>
                           
                           
                           							<nav>
                           
                           								<ul class="sf-menu sf-js-enabled sf-arrows">
                           									<li id="menu-item-6038"
                           										class="menu-item-scroll menu-item menu-item-type-custom menu-item-object-custom menu-item-6038">
                           										<a href="https://mothership.tv/#about-us">About Us</a></li>
                           									<li id="menu-item-6039"
                           										class="menu-item-scroll menu-item menu-item-type-custom menu-item-object-custom menu-item-6039">
                           										<a href="https://mothership.tv/#projects">Projects</a></li>
                           									<li id="menu-item-6057"
                           										class="menu-item menu-item-type-post_type menu-item-object-page menu-item-6057">
                           										<a href="https://mothership.tv/people/">People</a></li>
                           									<li id="menu-item-6056"
                           										class="menu-item menu-item-type-post_type menu-item-object-page menu-item-6056">
                           										<a href="https://mothership.tv/contact-us/">Contact</a></li>
                           								</ul>
                           
                           
                           								<ul class="buttons sf-menu" data-user-set-ocm="off">
                           
                           
                           								</ul>
                           
                           							</nav>
                           
                           
                           						</div><!--/span_9-->
                           
                           
                           					</div><!--/row-->
                           				</div><!--/container-->
                           			</header>
                           
                           			<div class="bg-color-stripe"></div>
                           		</div>
                           
                           
                           		<div id="ajax-content-wrap">
                           
                           
                           			<div id="full_width_portfolio">
                           				<div class="container-wrap" data-nav-pos="after_project" style="padding-bottom: 0px;">
                           
                           
                           					<div class="container main-content">
                           						<div class="row">
                           
                           
                           							<div class="post-area col span_12">
                           
                           								<div id="portfolio-extra">
                           									<div id="fws_66d7aedd913e3" data-midnight="dark" data-bg-mobile-hidden=""
                           										class="wpb_row vc_row-fluid vc_row standard_section full-width-section"
                           										style="padding-top: 0px; padding-bottom: 0px; ">
                           										<div class="row-bg-wrap" data-bg-animation="none">
                           											<div class="inner-wrap">
                           												<div class="row-bg" style=""></div>
                           											</div>
                           											<div class="row-bg-overlay"></div>
                           										</div>
                           										<div class="col span_12 dark left">
                           											<div class="vc_col-sm-12 wpb_column column_container vc_column_container col no-extra-padding instance-0"
                           												data-t-w-inherits="default" data-border-radius="none"
                           												data-shadow="none" data-border-animation=""
                           												data-border-animation-delay="" data-border-width="none"
                           												data-border-style="solid" data-border-color="" data-bg-cover=""
                           												data-padding-pos="all" data-has-bg-color="false" data-bg-color=""
                           												data-bg-opacity="1" data-hover-bg="" data-hover-bg-opacity="1"
                           												data-animation="" data-delay="0">
                           												<div class="column-bg-overlay"></div>
                           												<div class="vc_column-inner">
                           													<div class="wpb_wrapper">
                           														<div data-transition="slide" data-overall_style="classic"
                           															data-flexible-height=""
                           															data-animate-in-effect="zoom-out" data-fullscreen="true"
                           															data-button-sizing="large"
                           															data-button-styling="btn_with_count"
                           															data-autorotate="8000" data-parallax="false"
                           															data-parallax-disable-mobile=""
                           															data-caption-trans="fade_in_from_bottom"
                           															data-parallax-style="bg_only" data-bg-animation="none"
                           															data-full-width="true"
                           															class="nectar-slider-wrap nectar-slider-enabled loaded first-nectar-slider first-section"
                           															id="nectar-slider-instance-1"
                           															style="left: -842.5px; margin-left: 0px; width: 3055px; height: 1608px;"
                           															autoplay-id="0">
                           															<div class="swiper-container" data-tho="auto"
                           																data-tco="auto" data-pho="auto" data-pco="auto"
                           																data-loop="false" data-height="1608"
                           																data-min-height="" data-arrows="false"
                           																data-bullets="false" data-bullet_style="see_through"
                           																data-bullet_position="bottom"
                           																data-desktop-swipe="false" data-settings=""
                           																style="width: 3055px; background-color: rgb(0, 0, 0); height: 1609px;">
                           																<div class="swiper-wrapper no-transform"
                           																	style="width: 3055px; height: 1609px;">
                           																	<div class="swiper-slide swiper-slide-visible swiper-slide-active no-transform"
                           																		data-desktop-content-width="auto"
                           																		data-tablet-content-width="auto"
                           																		data-bg-alignment="center"
                           																		data-color-scheme="light" data-x-pos="left"
                           																		data-y-pos="middle"
                           																		style="background-color: rgb(0, 0, 0); width: 3055px; height: 1609px;">
                           																		<div class="slide-bg-wrap">
                           																			<div class="image-bg"
                           																				style="background-image: url(https://mothership.tv/wp-content/uploads/2021/10/RYKTER-Scene-1.jpg);">
                           																				&nbsp; </div>
                           																		</div>
                           																		<div class="video-texture"
                           																			style="opacity: 1;"> </div>
                           																	</div> <!--/swiper-slide-->
                           																</div>
                           																<div class="nectar-slider-loading "
                           																	style="display: none;"> <span
                           																		class="loading-icon none"> </span> </div>
                           															</div>
                           
                           														</div>
                           														<div id="row-single-project-content" data-midnight=""
                           															data-column-margin="default" data-bg-mobile-hidden=""
                           															class="wpb_row vc_row-fluid vc_row inner_row standard_section    "
                           															style="padding-top: 0px; padding-bottom: 0px; ">
                           															<div class="row-bg-wrap">
                           																<div class="row-bg   " style=""></div>
                           															</div>
                           															<div class="col span_12  left" style="height: 486.6px;">
                           																<div class="vc_col-sm-6 col-main-info wpb_column column_container vc_column_container col no-extra-padding instance-1"
                           																	data-t-w-inherits="default" data-shadow="none"
                           																	data-border-radius="none"
                           																	data-border-animation=""
                           																	data-border-animation-delay=""
                           																	data-border-width="none"
                           																	data-border-style="solid" data-border-color=""
                           																	data-bg-cover="" data-padding-pos="all"
                           																	data-has-bg-color="false" data-bg-color=""
                           																	data-bg-opacity="1" data-hover-bg=""
                           																	data-hover-bg-opacity="1" data-animation=""
                           																	data-delay="0">
                           																	<div class="column-bg-overlay"></div>
                           																	<div class="vc_column-inner">
                           																		<div class="wpb_wrapper">
                           																			<h1 style="text-align: left"
                           																				class="vc_custom_heading">Rumours
                           																				(Rykter)</h1>
                           																			<div style="text-align: left"
                           																				class="vc_custom_heading category-project-info vc_custom_1634893990759">
                           																				Drama</div>
                           																			<div
                           																				class="wpb_text_column wpb_content_element  category-project-description">
                           																				<div class="wpb_wrapper">
                           																					<p>“Rumours” is a drama series
                           																						commissioned by NRK. This is
                           																						an originally developed
                           																						story from Mothership
                           																						Entertainment aiming at a
                           																						young audience. The shooting
                           																						of this series will take
                           																						place in the western part of
                           																						Norway in first half of
                           																						2022.</p>
                           																				</div>
                           																			</div>
                           
                           
                           
                           
                           																		</div>
                           																	</div>
                           																</div>
                           
                           																<div class="vc_col-sm-6 col-project-details wpb_column column_container vc_column_container col no-extra-padding instance-2"
                           																	data-t-w-inherits="default" data-shadow="none"
                           																	data-border-radius="none"
                           																	data-border-animation=""
                           																	data-border-animation-delay=""
                           																	data-border-width="none"
                           																	data-border-style="solid" data-border-color=""
                           																	data-bg-cover="" data-padding-pos="all"
                           																	data-has-bg-color="false" data-bg-color=""
                           																	data-bg-opacity="1" data-hover-bg=""
                           																	data-hover-bg-opacity="1" data-animation=""
                           																	data-delay="0">
                           																	<div class="column-bg-overlay"></div>
                           																	<div class="vc_column-inner">
                           																		<div class="wpb_wrapper">
                           																			<h3 style="text-align: left"
                           																				class="vc_custom_heading project-details-header">
                           																				Creator</h3>
                           																			<div
                           																				class="wpb_text_column wpb_content_element  project-details-content">
                           																				<div class="wpb_wrapper">
                           																					<p>Christoffer Ebbesen</p>
                           																				</div>
                           																			</div>
                           
                           
                           
                           																			<h3 style="text-align: left"
                           																				class="vc_custom_heading project-details-header">
                           																				Producer</h3>
                           																			<div
                           																				class="wpb_text_column wpb_content_element  project-details-content">
                           																				<div class="wpb_wrapper">
                           																					<p>Linda Bolstad Strønen</p>
                           																				</div>
                           																			</div>
                           
                           
                           
                           
                           																		</div>
                           																	</div>
                           																</div>
                           															</div>
                           														</div>
                           													</div>
                           												</div>
                           											</div>
                           										</div>
                           									</div>
                           								</div>
                           							</div><!--/post-area-->
                           
                           
                           						</div>
                           					</div><!--/container-->
                           					<div class="bottom_controls">
                           						<div class="container">
                           							<div id="portfolio-nav">
                           								<a class="link-all-projects" href="http://mothership.tv/all-projects/"
                           									title="See All Projects">
                           									<span class="link-all-text">See All Projects</span>
                           									<svg class="next-arrow" xmlns="http://www.w3.org/2000/svg"
                           										xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 39 12">
                           										<line class="top" x1="23" y1="-0.5" x2="29.5" y2="6.5" stroke="#ffffff;">
                           										</line>
                           										<line class="bottom" x1="23" y1="12.5" x2="29.5" y2="5.5" stroke="#ffffff;">
                           										</line>
                           									</svg>
                           									<span class="line"></span>
                           								</a>
                           							</div>
                           						</div>
                           					</div>
                           				</div><!--/container-wrap-->
                           			</div><!--/if portfolio fullwidth-->
                           
                           			<div id="footer-outer" data-midnight="light" data-cols="4" data-custom-color="true"
                           				data-disable-copyright="false" data-matching-section-color="false" data-copyright-line="true"
                           				data-using-bg-img="false" data-bg-img-overlay="0.5" data-full-width="1"
                           				data-using-widget-area="true">
                           
                           
                           				<div id="footer-widgets" data-has-widgets="true" data-cols="4">
                           
                           					<div class="container">
                           
                           
                           						<div class="row">
                           
                           
                           							<div class="col span_3 one-fourths clear-both">
                           								<!-- Footer widget area 1 -->
                           								<div id="custom_html-2" class="widget_text widget widget_custom_html">
                           									<div class="textwidget custom-html-widget"><a id="logo"
                           											href="https://mothership.tv/" data-supplied-ml-starting-dark="false"
                           											data-supplied-ml-starting="false" data-supplied-ml="false">
                           											<img class="stnd default-logo" alt=""
                           												src="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg"
                           												srcset="https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 1x, https://mothership.tv/wp-content/uploads/2021/05/logo_mothership.svg 2x">
                           										</a></div>
                           								</div>
                           							</div><!--/span_3-->
                           
                           
                           							<div class="col span_3 one-fourths right-edge">
                           								<!-- Footer widget area 2 -->
                           								<div class="widget">
                           								</div>
                           
                           							</div><!--/span_3-->
                           
                           
                           
                           							<div class="col span_3 one-fourths clear-both">
                           								<!-- Footer widget area 3 -->
                           								<div id="text-5" class="widget widget_text">
                           									<div class="textwidget">
                           										<p>Strømgaten 4<br>
                           											5015 Bergen</p>
                           									</div>
                           								</div>
                           							</div><!--/span_3-->
                           
                           							<div class="col span_3 one-fourths right-edge">
                           								<!-- Footer widget area 4 -->
                           								<div id="text-6" class="widget widget_text">
                           									<div class="textwidget">
                           										<p>Tel: +47 901 44 922<br>
                           											Email: eldar@mothership.tv</p>
                           									</div>
                           								</div>
                           							</div><!--/span_3-->
                           
                           						</div><!--/row-->
                           
                           
                           					</div><!--/container-->
                           
                           				</div><!--/footer-widgets-->
                           
                           
                           				<div class="row" id="copyright" data-layout="centered">
                           
                           					<div class="container">
                           
                           						<div class="col span_5">
                           
                           
                           							<div class="widget">
                           							</div>
                           
                           							<p>© 2024 .
                           							</p>
                           
                           						</div><!--/span_5-->
                           
                           						<div class="col span_7 col_last">
                           							<ul class="social">
                           							</ul>
                           						</div><!--/span_7-->
                           
                           
                           					</div><!--/container-->
                           
                           				</div><!--/row-->
                           
                           
                           			</div><!--/footer-outer-->
                           
                           
                           			<div id="slide-out-widget-area-bg" class="fullscreen-alt dark">
                           				<div class="bg-inner"></div>
                           			</div>
                           
                           			<div id="slide-out-widget-area" class="fullscreen-alt" data-dropdown-func="default" data-back-txt="Back"
                           				style="">
                           
                           				<div class="inner-wrap">
                           					<div class="inner" data-prepend-menu-mobile="false">
                           
                           						<a class="slide_out_area_close" href="http://mothership.tv/portfolio/rumours/#">
                           							<span class="close-wrap"> <span class="close-line close-line1"></span> <span
                           									class="close-line close-line2"></span> </span> </a>
                           
                           
                           						<div class="off-canvas-menu-container mobile-only">
                           
                           
                           							<div class="menu-wrap menuwrapper">
                           								<ul class="menu menuopen">
                           									<li
                           										class="menu-item-scroll menu-item menu-item-type-custom menu-item-object-custom menu-item-6038">
                           										<a href="https://mothership.tv/#about-us">About Us</a></li>
                           									<li
                           										class="menu-item-scroll menu-item menu-item-type-custom menu-item-object-custom menu-item-6039">
                           										<a href="https://mothership.tv/#projects">Projects</a></li>
                           									<li
                           										class="menu-item menu-item-type-post_type menu-item-object-page menu-item-6057">
                           										<a href="https://mothership.tv/people/">People</a></li>
                           									<li
                           										class="menu-item menu-item-type-post_type menu-item-object-page menu-item-6056">
                           										<a href="https://mothership.tv/contact-us/">Contact</a></li>
                           
                           								</ul>
                           							</div>
                           
                           							<div class="menu-wrap menuwrapper">
                           								<ul class="menu secondary-header-items menuopen">
                           								</ul>
                           							</div>
                           						</div>
                           
                           					</div>
                           
                           					<div class="bottom-meta-wrap"></div><!--/bottom-meta-wrap-->
                           				</div> <!--/inner-wrap-->
                           			</div>
                           
                           		</div> <!--/ajax-content-wrap-->
                           
                           		<a id="to-top" class="
                           mobile-enabled	"><i class="fa fa-angle-up top-icon"></i><i class="fa fa-angle-up"></i></a>
                           	</div>
                           </div><!--/ocm-effect-wrap-->
                           """);
                break;
            }
        }

        base.OnInitialized();
    }


    private void RenderPage(string htmlContent)
    {
        PageRenderFragment = builder => builder.AddMarkupContent(0, htmlContent);
        InvokeAsync(StateHasChanged);
    }
}